namespace QuartzService;

using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


public class SuperWorker :
    BackgroundService
{
    readonly IMessageScheduler _messageScheduler;

    public SuperWorker(IMessageScheduler messageScheduler)
    {
        _messageScheduler = messageScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _messageScheduler.SchedulePublish(TimeSpan.FromSeconds(15), new DemoMessage() { Value = "Hello, World" }, cancellationToken: stoppingToken);
    }
}


public class DemoMessage
{
    public string Value { get; set; }
}


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