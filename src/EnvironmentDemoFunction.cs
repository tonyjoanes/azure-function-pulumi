using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HelloWorldFunction
{
    public class EnvironmentDemoFunction
    {
        private readonly ILogger _logger;

        public EnvironmentDemoFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EnvironmentDemoFunction>();
        }

        [Function("EnvironmentDemo")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req
        )
        {
            _logger.LogInformation("Environment demo function called.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            // Show how local.settings.json values become environment variables
            var environmentDemo = new
            {
                // These come from local.settings.json "Values" section
                fromEnvironmentVariables = new
                {
                    welcomeMessage = Environment.GetEnvironmentVariable("WelcomeMessage"),
                    maxRetries = Environment.GetEnvironmentVariable("MaxRetries"),
                    apiBaseUrl = Environment.GetEnvironmentVariable("ApiBaseUrl"),
                    functionsRuntime = Environment.GetEnvironmentVariable(
                        "FUNCTIONS_WORKER_RUNTIME"
                    ),
                    azureWebJobsStorage = Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                },

                // System environment variables
                systemEnvironment = new
                {
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    processorCount = Environment.ProcessorCount,
                    workingDirectory = Environment.CurrentDirectory,
                },

                // All environment variables starting with specific prefixes
                azureFunctionVars = Environment
                    .GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .Where(entry =>
                        entry
                            .Key.ToString()
                            ?.StartsWith("AZURE", StringComparison.OrdinalIgnoreCase) == true
                        || entry
                            .Key.ToString()
                            ?.StartsWith("FUNCTIONS", StringComparison.OrdinalIgnoreCase) == true
                    )
                    .ToDictionary(
                        entry => entry.Key.ToString() ?? "unknown",
                        entry => entry.Value?.ToString() ?? ""
                    ),

                // Our custom app settings (from local.settings.json)
                customAppSettings = Environment
                    .GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .Where(entry =>
                        new[]
                        {
                            "WelcomeMessage",
                            "MaxRetries",
                            "ApiBaseUrl",
                            "DatabaseConnectionString",
                        }.Contains(entry.Key.ToString())
                    )
                    .ToDictionary(
                        entry => entry.Key.ToString() ?? "unknown",
                        entry => entry.Value?.ToString() ?? ""
                    ),

                explanation = new
                {
                    localDevelopment = "Values from local.settings.json become environment variables",
                    azureProduction = "Application Settings in Azure become environment variables",
                    configurationSystem = "IConfiguration reads from environment variables",
                },
            };

            response.WriteString(
                JsonSerializer.Serialize(
                    environmentDemo,
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            return response;
        }
    }
}
