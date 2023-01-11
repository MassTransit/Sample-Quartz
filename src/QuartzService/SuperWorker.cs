namespace QuartzService;

using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


public class SuperWorker :
    BackgroundService
{
    readonly IServiceScopeFactory _scopeFactory;

    public SuperWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var messageScheduler = scope.ServiceProvider.GetRequiredService<IMessageScheduler>();

        await messageScheduler.SchedulePublish(TimeSpan.FromSeconds(15), new DemoMessage { Value = "Hello, World" }, stoppingToken);
    }
}