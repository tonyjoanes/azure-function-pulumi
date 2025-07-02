namespace HelloWorldFunction
{
    public class AppSettings
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public int MaxRetries { get; set; } = 3;
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string DatabaseConnectionString { get; set; } = string.Empty;
    }
}
 