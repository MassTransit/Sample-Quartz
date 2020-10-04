using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace NetCore
{
    public class PollExternalSystemConsumer : IConsumer<PollExternalSystem>
    {
        private readonly ILogger<PollExternalSystemConsumer> _logger;

        public PollExternalSystemConsumer(ILogger<PollExternalSystemConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<PollExternalSystem> context)
        {
            _logger.LogInformation("Polling external system");
            return Task.CompletedTask;
        }
    }
}