using MassTransit;
using MassTransitDemo.Contracts;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Consumer2.Consumers
{
    public class MessageConsumer2 :
         IConsumer<Message>
    {
        readonly ILogger<MessageConsumer2> _logger;

        public MessageConsumer2(ILogger<MessageConsumer2> logger)
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
