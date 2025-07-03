using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Config();
    var location = config.Get("location") ?? "East US";
    var environment = config.Get("environment") ?? "dev";
    var welcomeMessage =
        config.Get("welcomeMessage") ?? $"Hello from {environment.ToUpper()} environment!";

    // Create a resource group
    var resourceGroup = new ResourceGroup(
        "rg",
        new ResourceGroupArgs
        {
            Location = location,
            ResourceGroupName = $"rg-func-{environment}",
        }
    );

    // Create a storage account (required for Azure Functions)
    var storageAccount = new StorageAccount(
        "sa",
        new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountName = $"st{environment}{System.Guid.NewGuid().ToString("N")[..8]}",
            Location = location,
            Sku = new Pulumi.AzureNative.Storage.Inputs.SkuArgs { Name = SkuName.Standard_LRS },
            Kind = Pulumi.AzureNative.Storage.Kind.StorageV2,
        }
    );

    // Create Application Insights
    var appInsights = new Component(
        "ai",
        new ComponentArgs
        {
            ResourceGroupName = resourceGroup.Name,
            ResourceName = $"ai-func-{environment}",
            Location = location,
            ApplicationType = ApplicationType.Web,
            IngestionMode = IngestionMode.ApplicationInsights, // Fixed: Explicitly set to ApplicationInsights
            Kind = "web",
        }
    );

    // OPTION 1: Traditional Azure Functions (current approach)
    // Create an App Service Plan
    var appServicePlan = new AppServicePlan(
        "asp",
        new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Name = $"asp-func-{environment}",
            Location = location,
            Sku = new SkuDescriptionArgs
            {
                Name = "B1", // Basic tier - cheapest paid option
                Tier = "Basic",
            },
            Kind = "functionapp",
        }
    );

    // Create the Function App
    var functionApp = new WebApp(
        "func",
        new WebAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Name = $"func-{environment}-{System.Guid.NewGuid().ToString("N")[..8]}",
            Location = location,
            ServerFarmId = appServicePlan.Id,
            Kind = "functionapp",
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new NameValuePairArgs
                    {
                        Name = "AzureWebJobsStorage",
                        Value = Output
                            .Tuple(resourceGroup.Name, storageAccount.Name)
                            .Apply(t =>
                            {
                                var keys = ListStorageAccountKeys.Invoke(
                                    new ListStorageAccountKeysInvokeArgs
                                    {
                                        ResourceGroupName = t.Item1,
                                        AccountName = t.Item2,
                                    }
                                );
                                return keys.Apply(k =>
                                    $"DefaultEndpointsProtocol=https;AccountName={t.Item2};AccountKey={k.Keys[0].Value};EndpointSuffix=core.windows.net"
                                );
                            })
                            .Apply(o => o),
                    },
                    new NameValuePairArgs { Name = "FUNCTIONS_EXTENSION_VERSION", Value = "~4" },
                    new NameValuePairArgs
                    {
                        Name = "FUNCTIONS_WORKER_RUNTIME",
                        Value = "dotnet-isolated",
                    },
                    new NameValuePairArgs
                    {
                        Name = "APPINSIGHTS_INSTRUMENTATIONKEY",
                        Value = appInsights.InstrumentationKey,
                    },
                    new NameValuePairArgs
                    {
                        Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                        Value = appInsights.ConnectionString,
                    },
                    // Custom application settings
                    new NameValuePairArgs { Name = "WelcomeMessage", Value = welcomeMessage },
                    new NameValuePairArgs { Name = "MaxRetries", Value = "3" },
                    new NameValuePairArgs
                    {
                        Name = "ApiBaseUrl",
                        Value = "https://api.example.com",
                    },
                    new NameValuePairArgs
                    {
                        Name = "DatabaseConnectionString",
                        Value = "Server=localhost;Database=MyDb;Trusted_Connection=true;",
                    },
                    // Performance and reliability settings
                    new NameValuePairArgs
                    {
                        Name = "WEBSITE_RUN_FROM_PACKAGE",
                        Value = "1",
                    },
                    new NameValuePairArgs
                    {
                        Name = "WEBSITE_USE_PLACEHOLDER",
                        Value = "0",
                    },
                    new NameValuePairArgs
                    {
                        Name = "WEBSITE_ENABLE_SYNC_UPDATE_SITE",
                        Value = "true",
                    },
                },
                Use32BitWorkerProcess = false, // Enable 64-bit process
                HealthCheckPath = "/api/ConfigHealth", // Enable health check monitoring
            },
        }
    );

    /* OPTION 2: Azure Container Apps (Alternative if quota issues persist)
    // Uncomment this section if you want to try Container Apps instead
    
    using Pulumi.AzureNative.App;
    using Pulumi.AzureNative.App.Inputs;
    using Pulumi.AzureNative.OperationalInsights;
    
    // Create Log Analytics Workspace for Container Apps
    var logAnalytics = new Workspace("law", new WorkspaceArgs
    {
        ResourceGroupName = resourceGroup.Name,
        WorkspaceName = $"law-containerapp-{environment}",
        Location = location,
        Sku = new WorkspaceSkuArgs
        {
            Name = "PerGB2018"
        }
    });

    // Create Container Apps Environment
    var containerEnv = new ManagedEnvironment("cae", new ManagedEnvironmentArgs
    {
        ResourceGroupName = resourceGroup.Name,
        EnvironmentName = $"cae-azure-functions-{environment}",
        Location = location,
        AppLogsConfiguration = new AppLogsConfigurationArgs
        {
            Destination = "log-analytics",
            LogAnalyticsConfiguration = new LogAnalyticsConfigurationArgs
            {
                CustomerId = logAnalytics.CustomerId,
                SharedKey = logAnalytics.GetSharedKeys().Apply(keys => keys.PrimarySharedKey)
            }
        }
    });

    // Create Container App (Functions alternative)
    var containerApp = new ContainerApp("ca", new ContainerAppArgs
    {
        ResourceGroupName = resourceGroup.Name,
        ContainerAppName = $"ca-azure-functions-{environment}",
        Location = location,
        ManagedEnvironmentId = containerEnv.Id,
        Configuration = new ConfigurationArgs
        {
            Ingress = new IngressArgs
            {
                External = true,
                TargetPort = 80,
                Traffic = new[]
                {
                    new TrafficWeightArgs
                    {
                        Weight = 100,
                        LatestRevision = true
                    }
                }
            },
            Secrets = new[]
            {
                                 new SecretArgs
                 {
                     Name = "storage-connection",
                     Value = Output.Tuple(resourceGroup.Name, storageAccount.Name).Apply(t =>
                     {
                         var keys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
                         {
                             ResourceGroupName = t.Item1,
                             AccountName = t.Item2
                         });
                         return keys.Apply(k => $"DefaultEndpointsProtocol=https;AccountName={t.Item2};AccountKey={k.Keys[0].Value};EndpointSuffix=core.windows.net");
                     }).Apply(o => o)
                 }
            }
        },
        Template = new TemplateArgs
        {
            Containers = new[]
            {
                new ContainerArgs
                {
                    Name = "azure-functions",
                    Image = "mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0", // Use official Functions image
                    Resources = new ContainerResourcesArgs
                    {
                        Cpu = 0.25,
                        Memory = "0.5Gi"
                    },
                    Env = new[]
                    {
                        new EnvironmentVarArgs
                        {
                            Name = "AzureWebJobsStorage",
                            SecretRef = "storage-connection"
                        },
                        new EnvironmentVarArgs
                        {
                            Name = "FUNCTIONS_EXTENSION_VERSION",
                            Value = "~4"
                        },
                        new EnvironmentVarArgs
                        {
                            Name = "FUNCTIONS_WORKER_RUNTIME",
                            Value = "dotnet-isolated"
                        },
                        new EnvironmentVarArgs
                        {
                            Name = "WelcomeMessage",
                            Value = welcomeMessage
                        }
                    }
                }
            },
            Scale = new ScaleArgs
            {
                MinReplicas = 0,
                MaxReplicas = 10
            }
        }
    });
    */

    // Export the Function App URL
    return new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name,
        ["functionAppName"] = functionApp.Name,
        ["functionAppUrl"] = Output.Format($"https://{functionApp.DefaultHostName}"),
        ["storageAccountName"] = storageAccount.Name,
        ["appInsightsName"] = appInsights.Name,
        ["location"] = location,
        ["environment"] = environment,
    };
});
