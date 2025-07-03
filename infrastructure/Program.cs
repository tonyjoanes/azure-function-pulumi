using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Web;

return await Pulumi.Deployment.RunAsync(() =>
{
    // Get configuration (allows region override per environment)
    var config = new Pulumi.Config();
    var stackName = Pulumi.Deployment.Instance.StackName;
    var location = config.Get("location") ?? GetDefaultLocationForStack(stackName);
    
    // Helper function for environment-specific defaults
    static string GetDefaultLocationForStack(string stackName) => stackName switch
    {
        "dev" => "East US",
        "staging" => "West US 2", 
        "prod" => "Central US",
        _ => "East US"
    };
    
    // Create a Resource Group
    var resourceGroup = new ResourceGroup(
        "azure-function-rg",
        new ResourceGroupArgs { Location = location }
    );

    // Create a Storage Account (required for Azure Functions)
    var storageAccount = new StorageAccount(
        "azfuncstore",
        new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            Sku = new Pulumi.AzureNative.Storage.Inputs.SkuArgs { Name = SkuName.Standard_LRS },
            Kind = Pulumi.AzureNative.Storage.Kind.StorageV2,
        }
    );

    // Get the storage account keys
    var storageAccountKeys = ListStorageAccountKeys.Invoke(
        new ListStorageAccountKeysInvokeArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountName = storageAccount.Name,
        }
    );

    var primaryStorageKey = storageAccountKeys.Apply(keys => keys.Keys[0].Value);

    // Create storage connection string
    var storageConnectionString = Output.Format(
        $"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey={primaryStorageKey};EndpointSuffix=core.windows.net"
    );

    // Create Application Insights
    var appInsights = new Component(
        "azure-function-ai",
        new ComponentArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ApplicationType = ApplicationType.Web,
            Kind = "web",
            IngestionMode = IngestionMode.ApplicationInsights, // Avoid LogAnalytics workspace requirement
        }
    );

    // Create App Service Plan (Basic Plan - trying to avoid Dynamic VM quota issues)
    // NOTE: Trying B1 Basic first since subscription has 0 Dynamic VM quota
    // Basic plan: ~$13/month but more predictable than quota requests
    // To switch back to Consumption (Y1): requires Dynamic VMs quota increase
    var appServicePlan = new AppServicePlan(
        "azure-function-plan",
        new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            Sku = new Pulumi.AzureNative.Web.Inputs.SkuDescriptionArgs
            {
                Name = "B1", // Basic plan (may have different quota pool)
                Tier = "Basic",
            },
            Kind = "FunctionApp",
        }
    );

    // Create the Function App
    var functionApp = new WebApp(
        "azure-function-app",
        new WebAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ServerFarmId = appServicePlan.Id,
            Kind = "FunctionApp",
            SiteConfig = new Pulumi.AzureNative.Web.Inputs.SiteConfigArgs
            {
                AppSettings = new[]
                {
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "AzureWebJobsStorage",
                        Value = storageConnectionString,
                    },
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "FUNCTIONS_EXTENSION_VERSION",
                        Value = "~4",
                    },
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "FUNCTIONS_WORKER_RUNTIME",
                        Value = "dotnet-isolated",
                    },
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "APPINSIGHTS_INSTRUMENTATIONKEY",
                        Value = appInsights.InstrumentationKey,
                    },
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                        Value = appInsights.ConnectionString,
                    },
                    // Your custom application settings
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "WelcomeMessage",
                        Value = "Hello from Azure via Pulumi!",
                    },
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "MaxRetries",
                        Value = "5",
                    },
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "ApiBaseUrl",
                        Value = "https://prod-api.example.com",
                    },
                    new Pulumi.AzureNative.Web.Inputs.NameValuePairArgs
                    {
                        Name = "DatabaseConnectionString",
                        Value =
                            "Server=prod-server.database.windows.net;Database=ProdDB;Authentication=Active Directory Default;",
                    },
                },
                NetFrameworkVersion = "v6.0",
            },
        }
    );

    // Export important values
    return new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name,
        ["functionAppName"] = functionApp.Name,
        ["functionAppUrl"] = Output.Format($"https://{functionApp.DefaultHostName}"),
        ["storageAccountName"] = storageAccount.Name,
        ["appInsightsInstrumentationKey"] = appInsights.InstrumentationKey,
    };
});
