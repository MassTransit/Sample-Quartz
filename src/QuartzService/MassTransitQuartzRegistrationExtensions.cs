namespace QuartzService;

using MassTransit;
using MassTransit.QuartzIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


public static class MassTransitQuartzRegistrationExtensions
{
    /// <summary>
    /// Add the Quartz consumers to the bus, using <see cref="QuartzEndpointOptions"/> for configuration.
    /// </summary>
    /// <param name="configurator"></param>
    public static void AddQuartzConsumers(this IBusRegistrationConfigurator configurator)
    {
        configurator.AddOptions<QuartzEndpointOptions>();

        configurator.TryAddSingleton<QuartzEndpointDefinition>();

        configurator.AddConsumer<ScheduleMessageConsumer, ScheduleMessageConsumerDefinition>();
        configurator.AddConsumer<CancelScheduledMessageConsumer, CancelScheduledMessageConsumerDefinition>();
    }

    /// <summary>
    /// When manually configuring a receive endpoint, configure the Quartz consumers for this endpoint
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
    public static void ConfigureQuartzConsumers(this IReceiveEndpointConfigurator configurator, IBusRegistrationContext context)
    {
        configurator.ConfigureConsumer<ScheduleMessageConsumer>(context);
        configurator.ConfigureConsumer<CancelScheduledMessageConsumer>(context);
    }
}