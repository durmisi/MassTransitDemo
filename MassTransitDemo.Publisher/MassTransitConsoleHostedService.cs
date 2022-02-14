using MassTransit;
using MassTransitDemo.Common.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Publisher
{
    public class MassTransitConsoleHostedService :
        IHostedService
    {
        readonly IBusControl _bus;
        readonly ILogger _logger;

        public MassTransitConsoleHostedService(IBusControl bus, ILoggerFactory loggerFactory)
        {
            _bus = bus;
            _logger = loggerFactory.CreateLogger<MassTransitConsoleHostedService>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting bus");
            await _bus.StartAsync(cancellationToken)
                .ConfigureAwait(false);


            var i = 0;

            while (true)
            {
                i++;
                await _bus.Publish(new Message() { Text = "Testing " + i.ToString() }, cancellationToken)
                    .ConfigureAwait(false);

                await Task.Delay(1000);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping bus");
            return _bus.StopAsync(cancellationToken);
        }
    }
}
