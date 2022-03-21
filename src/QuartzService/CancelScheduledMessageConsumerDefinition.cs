using MassTransit;
using MassTransit.QuartzIntegration;
using MassTransit.Scheduling;

namespace QuartzService;

public class CancelScheduledMessageConsumerDefinition :
    ConsumerDefinition<CancelScheduledMessageConsumer>
{
    readonly QuartzEndpointDefinition _endpointDefinition;

    public CancelScheduledMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
    {
        _endpointDefinition = endpointDefinition;

        EndpointDefinition = endpointDefinition;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CancelScheduledMessageConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(5, 250));

        consumerConfigurator.Message<CancelScheduledMessage>(m => m.UsePartitioner(_endpointDefinition.Partition, p => p.Message.TokenId));
    }
}