using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MassTransit;
using MassTransitDemo.Common.Helpers;
using MassTransitDemo.Contracts;
using Microsoft.Extensions.Options;

namespace MassTransitDemo.Common
{
    public class MassTransitConfiguration
    {
        public static IBusControl ConfigureBus(IBusRegistrationContext context)
        {
            AppConfig appConfig = context.GetRequiredService<IOptions<AppConfig>>().Value;

            if (appConfig is null)
            {
                throw new ArgumentNullException(nameof(appConfig));
            }

            switch (appConfig.Provider.ToLower())
            {
                case "rabbitmq":
                    return ConfigureUsingRabbitMq(context, appConfig.RabbitMQ);

                case "azureservicebus":
                    return ConfigureUsingAzureServiceBus(context, appConfig.AzureServiceBus);

                default:
                    throw new InvalidOperationException("Provider {provider} is not supported. Please check your configuration and try again.");
            }
        }


        public static IBusControl ConfigureUsingAzureServiceBus(IBusRegistrationContext context, AzureServiceBusConfiguration azureServiceBusConfiguration)
        {
            if (azureServiceBusConfiguration is null)
            {
                throw new ArgumentNullException(nameof(azureServiceBusConfiguration));
            }

            var connectionString = azureServiceBusConfiguration.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("Please provide a valid connection string");
            }

            return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                // specify the message "message-topic" to be sent to a specific topic
                cfg.Message<Message>(configTopology =>
                {
                    configTopology.SetEntityName("message-topic");
                });

                cfg.Host(connectionString, hostConfig =>
                {
                    // This is optional, but you can specify the protocol to use.
                    hostConfig.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpWebSockets;
                });

                cfg.ConfigureEndpoints(context);
            });
        }


        public static IBusControl ConfigureUsingRabbitMq(IBusRegistrationContext context, RabbitMQConfiguration rabbitMQConfig)
        {
            if (rabbitMQConfig is null)
            {
                throw new ArgumentNullException(nameof(rabbitMQConfig));
            }

            X509Certificate2? x509Certificate2 = null;

            if (rabbitMQConfig.SSLActive)
            {
                x509Certificate2 = X509CertificateHelper.GetCertificate(StoreName.My,
                    StoreLocation.LocalMachine,
                    rabbitMQConfig.SSLThumbprint);
            }

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(rabbitMQConfig.Host, rabbitMQConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMQConfig.Username);
                    h.Password(rabbitMQConfig.Password);

                    if (rabbitMQConfig.SSLActive)
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
                                        .FirstOrDefault(cert =>
                                        cert.Thumbprint.ToLower() == rabbitMQConfig.SSLThumbprint.ToLower());

                                return serverCertificate ?? throw new Exception("Wrong certificate");
                            };
                        });
                    }
                });

                cfg.ConfigureEndpoints(context);
            });

        }

    }
}
