using MassTransit.Scheduling;

namespace NetCore
{
    public class PollExternalSystemSchedule : DefaultRecurringSchedule
    {
        public PollExternalSystemSchedule()
        {
            CronExpression = "0 0/1 * 1/1 * ? *"; // this means every minute
        }
    }
}