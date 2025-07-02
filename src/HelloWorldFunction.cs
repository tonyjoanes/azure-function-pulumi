using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HelloWorldFunction
{
    public class HelloWorldFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly AppSettings _appSettings;

        public HelloWorldFunction(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            IOptions<AppSettings> appSettings
        )
        {
            _logger = loggerFactory.CreateLogger<HelloWorldFunction>();
            _configuration = configuration;
            _appSettings = appSettings.Value;
        }

        [Function("HelloWorld")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req
        )
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Method 1: Using IConfiguration directly
            var welcomeMessage = _configuration["WelcomeMessage"] ?? "Default Welcome Message";

            // Method 2: Using strongly-typed configuration (recommended)
            var maxRetries = _appSettings.MaxRetries;
            var apiBaseUrl = _appSettings.ApiBaseUrl;

            // Method 3: Reading with type conversion
            var maxRetriesFromConfig = _configuration.GetValue<int>("MaxRetries", 5);

            // Log configuration values (be careful with sensitive data!)
            _logger.LogInformation("WelcomeMessage: {WelcomeMessage}", welcomeMessage);
            _logger.LogInformation("MaxRetries from AppSettings: {MaxRetries}", maxRetries);
            _logger.LogInformation(
                "MaxRetries from IConfiguration: {MaxRetries}",
                maxRetriesFromConfig
            );

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var responseObject = new
            {
                message = welcomeMessage,
                maxRetries = maxRetries,
                apiBaseUrl = apiBaseUrl,
                timestamp = DateTime.UtcNow,
                configMethods = new
                {
                    fromIConfiguration = _configuration["WelcomeMessage"],
                    fromStronglyTyped = _appSettings.WelcomeMessage,
                    fromGetValue = _configuration.GetValue<string>("WelcomeMessage", "fallback"),
                },
            };

            response.WriteString(
                JsonSerializer.Serialize(
                    responseObject,
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            return response;
        }
    }
}
