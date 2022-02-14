namespace MassTransitDemo.Common
{
    public class AppConfig
    {
        public string Provider { get; set; }

        public RabbitMQConfiguration RabbitMQ { get; set; }

        public AzureServiceBusConfiguration AzureServiceBus { get; set; }

    }

    public class RabbitMQConfiguration
    {
        public string Host { get; set; }
        public string VirtualHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool SSLActive { get; set; }
        public string SSLThumbprint { get; set; }
    }

    public class AzureServiceBusConfiguration
    {
        public string ConnectionString { get; set; }
    }
}
