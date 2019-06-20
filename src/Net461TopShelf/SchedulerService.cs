using MassTransit;
using MassTransit.QuartzIntegration;
using MassTransit.Util;
using Quartz;
using Quartz.Impl;
using System;

namespace Net461TopShelf
{
    public class SchedulerService
    {
        IScheduler _scheduler;
        IBusControl _busControl;
        BusHandle _busHandle;

        public SchedulerService(IBusControl busControl, IScheduler scheduler)
        {
            _busControl = busControl;
            _scheduler = scheduler;
        }

        public bool Start()
        {
            try
            {
                _busHandle = TaskUtil.Await(() => _busControl.StartAsync());

                _scheduler.JobFactory = new MassTransitJobFactory(_busControl);

                TaskUtil.Await(() => _scheduler.Start());
            }
            catch (Exception)
            {
                TaskUtil.Await(() => _scheduler.Shutdown());
                throw;
            }

            Console.WriteLine("Started");
            return true;
        }

        public bool Stop()
        {
            TaskUtil.Await(() => _scheduler.Standby());

            if (_busHandle != null)
                TaskUtil.Await(() => _busHandle.StopAsync());

            TaskUtil.Await(() => _scheduler.Shutdown());

            Console.WriteLine("Stopped");
            return true;
        }
    }
}
