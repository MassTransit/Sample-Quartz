namespace QuartzService;

using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;


public class SampleConsumer :
    IConsumer<DemoMessage>
{
    readonly ILogger<SampleConsumer> _logger;

    public SampleConsumer(ILogger<SampleConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<DemoMessage> context)
    {
        _logger.LogInformation("Received scheduled message: {Value}", context.Message.Value);

        return Task.CompletedTask;
    }
}