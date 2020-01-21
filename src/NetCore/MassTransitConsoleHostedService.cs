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
        IScheduler _scheduler;

        public MassTransitConsoleHostedService(IBusControl bus, ILoggerFactory loggerFactory, IScheduler scheduler)
        {
            _bus = bus;

            _logger = loggerFactory.CreateLogger<MassTransitConsoleHostedService>();

            _scheduler = scheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting bus");
            await _bus.StartAsync(cancellationToken).ConfigureAwait(false);

            _scheduler.JobFactory = new MassTransitJobFactory(_bus);
            try
            {
                _logger.LogInformation("Starting scheduler");
                await _scheduler.Start();
            }
            catch (Exception)
            {
                await _scheduler.Shutdown();

                throw;
            }

            _logger.LogInformation("Started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler.Standby();

            _logger.LogInformation("Stopping");
            await _bus.StopAsync(cancellationToken);

            await _scheduler.Shutdown();

            _logger.LogInformation("Stopped");
        }
    }
}
