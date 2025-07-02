using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelloWorldFunction
{
    public class ConfigDemoFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly AppSettings _appSettings;

        public ConfigDemoFunction(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IOptions<AppSettings> appSettings
        )
        {
            _logger = loggerFactory.CreateLogger<ConfigDemoFunction>();
            _configuration = configuration;
            _appSettings = appSettings.Value;
        }

        [Function("ConfigDemo")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req
        )
        {
            _logger.LogInformation("Configuration demo function called.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            // Demonstrate different configuration scenarios
            var configDemo = new
            {
                // 1. Simple string configuration
                welcomeMessage = _appSettings.WelcomeMessage,

                // 2. Numeric configuration with fallback
                maxRetries = _configuration.GetValue<int>("MaxRetries", 3),

                // 3. Boolean configuration (not in our settings, showing fallback)
                enableDebug = _configuration.GetValue<bool>("EnableDebug", false),

                // 4. Complex configuration sections (you can extend this)
                apiConfiguration = new
                {
                    baseUrl = _appSettings.ApiBaseUrl,
                    timeout = _configuration.GetValue<int>("ApiTimeout", 30),
                },

                // 5. Environment-specific behavior
                environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
                    ?? "Development",

                // 6. Connection string handling (sanitized for demo)
                hasConnectionString = !string.IsNullOrEmpty(_appSettings.DatabaseConnectionString),
                connectionStringLength = _appSettings.DatabaseConnectionString?.Length ?? 0,

                // 7. All environment variables starting with custom prefix
                customSettings = Environment
                    .GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .Where(entry => entry.Key.ToString()?.StartsWith("CUSTOM_") == true)
                    .ToDictionary(entry => entry.Key.ToString() ?? "unknown", entry => entry.Value?.ToString() ?? ""),

                configurationSources = new
                {
                    note = "In Azure, these come from Application Settings",
                    localNote = "In local development, these come from local.settings.json",
                },
            };

            response.WriteString(
                JsonSerializer.Serialize(
                    configDemo,
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            return response;
        }

        [Function("ConfigHealth")]
        public HttpResponseData Health(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req
        )
        {
            _logger.LogInformation("Configuration health check called.");

            var response = req.CreateResponse(HttpStatusCode.OK);

            // Validate critical configuration
            var configErrors = new List<string>();

            if (string.IsNullOrEmpty(_appSettings.WelcomeMessage))
                configErrors.Add("WelcomeMessage is not configured");

            if (_appSettings.MaxRetries <= 0)
                configErrors.Add("MaxRetries must be greater than 0");

            if (string.IsNullOrEmpty(_appSettings.ApiBaseUrl))
                configErrors.Add("ApiBaseUrl is not configured");

            var healthStatus = new
            {
                status = configErrors.Count == 0 ? "Healthy" : "Unhealthy",
                errors = configErrors,
                timestamp = DateTime.UtcNow,
                configurationValidated = new
                {
                    welcomeMessage = !string.IsNullOrEmpty(_appSettings.WelcomeMessage),
                    maxRetries = _appSettings.MaxRetries > 0,
                    apiBaseUrl = !string.IsNullOrEmpty(_appSettings.ApiBaseUrl),
                },
            };

            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(
                JsonSerializer.Serialize(
                    healthStatus,
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            return response;
        }
    }
}
