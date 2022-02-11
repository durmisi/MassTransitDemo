using MassTransit;
using MassTransit.Definition;
using MassTransit.ConsumeConfigurators;
using GreenPipes;
using MassTransitDemo.Consumer.Consumers;

namespace MassTransitDemo.Consumer2.Consumers
{
    class MessageConsumerDefinition :
       ConsumerDefinition<MessageConsumer>
    {
        public MessageConsumerDefinition()
        {
            // override the default endpoint name
            EndpointName = "MessageConsumer";

            // limit the number of messages consumed concurrently
            // this applies to the consumer only, not the endpoint
            ConcurrentMessageLimit = 8;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<MessageConsumer> consumerConfigurator)
        {
            // configure message retry with millisecond intervals
            endpointConfigurator.UseMessageRetry(r => r.Intervals(100, 200, 500, 800, 1000));

            // use the outbox to prevent duplicate events from being published
            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}
