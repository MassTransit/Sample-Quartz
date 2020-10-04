using MassTransit;
using MassTransit.QuartzIntegration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetCore
{
    public class MassTransitConsoleHostedService : IHostedService
    {
        readonly IBusControl _bus;
        readonly ILogger _logger;
        readonly ISchedulerFactory _schedulerFactory;
        IScheduler _scheduler;

        public MassTransitConsoleHostedService(
            IBusControl bus,
            ILoggerFactory loggerFactory,
            ISchedulerFactory schedulerFactory)
        {
            _bus = bus;

            _logger = loggerFactory.CreateLogger<MassTransitConsoleHostedService>();

            _schedulerFactory = schedulerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting bus");
            await _bus.StartAsync(cancellationToken).ConfigureAwait(false);

            _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            try
            {
                _logger.LogInformation("Starting scheduler");
                await _scheduler.Start(cancellationToken);

                // an example of creating a recurring send
                var uri = new Uri("queue:quartz-scheduler");
                var schedulerEndpoint = await _bus.GetSendEndpoint(uri);
    
                var scheduledRecurringMessage = await schedulerEndpoint.ScheduleRecurringSend(
                    destinationAddress: uri, 
                    new PollExternalSystemSchedule(),
                    new PollExternalSystem(),
                    cancellationToken);
            }
            catch (Exception)
            {
                await _scheduler.Shutdown(cancellationToken);
                throw;
            }

            _logger.LogInformation("Started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler.Standby(cancellationToken);

            _logger.LogInformation("Stopping");
            await _bus.StopAsync(cancellationToken);

            await _scheduler.Shutdown(cancellationToken);

            _logger.LogInformation("Stopped");
        }
    }
}
