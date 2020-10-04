using GreenPipes;
using MassTransit;
using MassTransit.QuartzIntegration;
using MassTransit.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz.Spi;

namespace NetCore
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = CreateHostBuilder(args);

            if (isService)
            {
                await builder.UseWindowsService().Build().RunAsync();
            }
            else
            {
                await builder.RunConsoleAsync();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables();

                if (args != null)
                    config.AddCommandLine(args);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
                
                services.Configure<QuartzOptions>(hostContext.Configuration.GetSection("Quartz"));
                
                services.AddQuartz(q =>
                {
                    q.ScheduleJob<MaintenanceJob>(
                        trigger => trigger.WithSimpleSchedule(x => x
                                .WithInterval(TimeSpan.FromSeconds(30))
                            )
                            .StartNow());
                    
                    q.UsePersistentStore(store =>
                    {
                        store.UseSqlServer(db =>
                        {
                            db.ConnectionString = hostContext.Configuration.GetConnectionString("scheduler-db");
                        });
                        
                        // JSON serializer is preferred due to being more secure and performant
                        store.UseJsonSerializer();
                        
                        // we can also configure clustering, you need this if you have multiple nodes
                        // store.UseClustering();
                    });
                });

                // these should probably be part of some UseMessageScheduler()
                services.Replace(ServiceDescriptor.Singleton(typeof(IJobFactory), typeof(MassTransitJobFactory)));
                services.AddTransient<ScheduledMessageJob>();
                services.Configure<QuartzOptions>(options =>
                {
                    options.JobFactory.Type = typeof(MassTransitJobFactory);
                });

                services.TryAddSingleton(provider => provider.GetRequiredService<ISchedulerFactory>().GetScheduler().GetAwaiter().GetResult());
                
                // Service Bus
                services.AddMassTransit(cfg =>
                {
                    cfg.AddConsumers(typeof(ScheduleMessageConsumer), typeof(CancelScheduledMessageConsumer), typeof(PollExternalSystemConsumer));
                    cfg.AddBus(ConfigureBus);
                });

                services.AddHostedService<MassTransitConsoleHostedService>();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
            });



        static IBusControl ConfigureBus(IServiceProvider provider)
        {
            var options = provider.GetRequiredService<IOptions<AppConfig>>().Value;

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(options.Host, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.UseJsonSerializer(); // Because we are using json within Quartz for serializer type

                cfg.ReceiveEndpoint(options.QueueName, endpoint =>
                {
                    var partitionCount = Environment.ProcessorCount;
                    endpoint.PrefetchCount = (ushort) partitionCount;
                    var partitioner = endpoint.CreatePartitioner(partitionCount);

                    endpoint.Consumer<ScheduleMessageConsumer>(provider, x =>
                        x.Message<ScheduleMessage>(m => m.UsePartitioner(partitioner, p => p.Message.CorrelationId)));
                    endpoint.Consumer<CancelScheduledMessageConsumer>(provider,
                        x => x.Message<CancelScheduledMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));
                    
                    endpoint.Consumer<PollExternalSystemConsumer>(provider);
                    
                    cfg.UseMessageScheduler(endpoint.InputAddress);
                });
            });
        }
    }
}
