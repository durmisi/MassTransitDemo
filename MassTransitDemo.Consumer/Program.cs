using System.Diagnostics;
using MassTransit;
using MassTransitDemo.Common;
using MassTransitDemo.Consumer.Consumers;
using MassTransitDemo.Consumer2.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Consumer
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));

                    services.AddMassTransit(cfg =>
                     {
                         cfg.SetKebabCaseEndpointNameFormatter();

                         cfg.AddConsumer<MessageConsumer>(typeof(MessageConsumerDefinition));

                         cfg.AddBus(context => MassTransitConfiguration.ConfigureBus(context));
                     });

                    services.AddHostedService<MassTransitConsoleHostedService>();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            if (isService)
            {
                await builder.UseWindowsService().Build().RunAsync();
                //await builder.UseSystemd().Build().RunAsync(); 
                // For Linux, replace the nuget package: "Microsoft.Extensions.Hosting.WindowsServices"
                // with "Microsoft.Extensions.Hosting.Systemd", and then use this line instead
            }
            else
            {
                await builder.RunConsoleAsync();
            }

        }

    }
}
