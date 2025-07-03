using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Web;

return await Pulumi.Deployment.RunAsync(() =>
{
    // Create a Resource Group
    var resourceGroup = new ResourceGroup(
        "azure-function-rg",
        new ResourceGroupArgs { Location = "East US" }
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

    // Create App Service Plan (Consumption Plan for serverless Azure Functions)
    // NOTE: Azure Functions require at least Y1 (Consumption) or B1 (Basic) - Free tier NOT supported
    // If you get quota errors, request "Dynamic VMs" quota increase in Azure Portal
    // Alternative: Use B1 Basic plan if Y1 fails (requires "Basic VMs" quota)
    var appServicePlan = new AppServicePlan(
        "azure-function-plan",
        new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            Sku = new Pulumi.AzureNative.Web.Inputs.SkuDescriptionArgs
            {
                Name = "Y1", // Consumption plan (serverless, pay-per-execution)
                Tier = "Dynamic",
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
