using MassTransit;
using MassTransit.QuartzIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QuartzService;

public static class MassTransitQuartzRegistrationExtensions
{
    public static void AddQuartz(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddOptions<QuartzEndpointOptions>();

        configurator.TryAddSingleton<QuartzEndpointDefinition>();

        configurator.AddConsumer<ScheduleMessageConsumer, ScheduleMessageConsumerDefinition>();
        configurator.AddConsumer<CancelScheduledMessageConsumer, CancelScheduledMessageConsumerDefinition>();
    }
}