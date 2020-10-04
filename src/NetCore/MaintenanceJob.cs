using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace NetCore
{
    /// <summary>
    /// Custom job that runs alongside MassTransit.
    /// </summary>
    public class MaintenanceJob : IJob
    {
        private readonly ILogger<MaintenanceJob> _logger;

        public MaintenanceJob(ILogger<MaintenanceJob> logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Doing some maintenance work");
            return Task.CompletedTask;
        }
    }
}