using MassTransit;
using MassTransitDemo.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Consumer.Consumers
{
    public class MessageConsumer :
         IConsumer<Message>
    {
        readonly ILogger<MessageConsumer> _logger;

        public MessageConsumer(ILogger<MessageConsumer> logger)
        {
            _logger = logger;

            _logger.LogInformation("MessageConsumer is listening for new messages.");

        }

        public Task Consume(ConsumeContext<Message> context)
        {
            _logger.LogInformation("Received Text: {Text}", context.Message.Text);

            return Task.CompletedTask;
        }
    }
}