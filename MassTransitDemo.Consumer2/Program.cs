using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MassTransitDemo.Consumer2.Consumers;
using MassTransitDemo.Consumer2;

namespace MassTransitDemo.Consumer2
{
    public class Program
    {
        public static AppConfig AppConfig { get; set; }

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
                        cfg.AddConsumer<MessageConsumer2>(typeof(MessageConsumer2Definition));
                        cfg.AddBus(context => ConfigureBus(context));
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

        static IBusControl ConfigureBus(IBusRegistrationContext context)
        {
            AppConfig = context.GetRequiredService<IOptions<AppConfig>>().Value;

            X509Certificate2? x509Certificate2 = null;

            if (AppConfig.SSLActive)
            {
                x509Certificate2 = GetCertificate(StoreName.My, StoreLocation.LocalMachine, AppConfig.SSLThumbprint);
            }

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(AppConfig.Host, AppConfig.VirtualHost, h =>
                {
                    h.Username(AppConfig.Username);
                    h.Password(AppConfig.Password);

                    if (AppConfig.SSLActive)
                    {
                        h.UseSsl(ssl =>
                        {
                            ssl.ServerName = Dns.GetHostName();
                            ssl.AllowPolicyErrors(SslPolicyErrors.RemoteCertificateNameMismatch);
                            ssl.Certificate = x509Certificate2;
                            ssl.Protocol = SslProtocols.Tls12;

                            ssl.CertificateSelectionCallback
                            = (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate? remoteCertificate, string[] acceptableIssuers) =>
                            {
                                var serverCertificate = localCertificates.OfType<X509Certificate2>()
                                        .FirstOrDefault(cert => cert.Thumbprint.ToLower() == AppConfig.SSLThumbprint.ToLower());

                                return serverCertificate ?? throw new Exception("Wrong certificate");
                            };
                        });
                    }
                });

                cfg.ConfigureEndpoints(context);
            });

        }

        private static X509Certificate2? GetCertificate(StoreName storeName, StoreLocation storeLocation, string sSLThumbprint)
        {
            X509Certificate2? x509Certificate = null;

            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                X509Certificate2Collection certificatesInStore = store.Certificates;

                x509Certificate = certificatesInStore.OfType<X509Certificate2>()
                    .FirstOrDefault(cert =>
                    {
                        return cert != null && string.Equals(
                               cert.Thumbprint?.ToLower(),
                               sSLThumbprint?.ToLower(),
                               StringComparison.OrdinalIgnoreCase
                           );
                    });
            }
            finally
            {
                store.Close();
            }

            return x509Certificate;
        }

    }
}
