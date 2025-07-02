using HelloWorldFunction;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // IConfiguration is automatically registered, but you can add custom services here
        services
            .AddOptions<AppSettings>()
            .Configure<IConfiguration>(
                (settings, configuration) =>
                {
                    configuration.Bind(settings);
                }
            );
    })
    .Build();

host.Run();
