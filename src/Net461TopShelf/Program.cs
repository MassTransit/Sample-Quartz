﻿using Autofac;
using GreenPipes;
using MassTransit;
using MassTransit.Context;
using MassTransit.QuartzIntegration;
using MassTransit.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Configuration;
using Topshelf;
using Topshelf.HostConfigurators;
using Topshelf.Logging;

namespace Net461TopShelf
{
    class Program
    {
        static int Main()
        {
            ConfigureSerilog();

            try
            {
                return (int)HostFactory.Run(Configure);
            }
            catch (Exception e)
            {
                if (Log.Logger == null || Log.Logger.GetType().Name == "SilentLogger")
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .CreateLogger();
                }

                Log.Logger.Fatal(e.ToString());
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static void Configure(HostConfigurator hostConfigurator)
        {
            hostConfigurator.Service<SchedulerService>(s =>
            {
                // TopShelf will create a new Service every time you Stop/Start within Windows Services.
                s.ConstructUsing(() =>
                {
                    var builder = new ContainerBuilder();

                    var serilogFactory = new SerilogLoggerFactory();

                    LogContext.ConfigureCurrentLogContext(serilogFactory);

                    // Service Bus
                    builder.AddMassTransit(mt =>
                    {
                        mt.UsingRabbitMq((context, cfg) =>
                        {
                            var scheduler = context.GetRequiredService<IScheduler>();

                            cfg.Host(ConfigurationManager.AppSettings["RabbitMQHost"], ConfigurationManager.AppSettings["RabbitMQVirtualHost"], h =>
                            {
                                h.Username(ConfigurationManager.AppSettings["RabbitMQUsername"]);
                                h.Password(ConfigurationManager.AppSettings["RabbitMQPassword"]);
                            });

                            cfg.UseJsonSerializer(); // Because we are using json within Quartz for serializer type

                            cfg.ReceiveEndpoint(ConfigurationManager.AppSettings["QueueName"], endpoint =>
                            {
                                var partitionCount = Environment.ProcessorCount;
                                endpoint.PrefetchCount = (ushort)(partitionCount);
                                var partitioner = endpoint.CreatePartitioner(partitionCount);

                                endpoint.Consumer(() => new ScheduleMessageConsumer(scheduler), x =>
                                    x.Message<ScheduleMessage>(m => m.UsePartitioner(partitioner, p => p.Message.CorrelationId)));
                                endpoint.Consumer(() => new CancelScheduledMessageConsumer(scheduler),
                                    x => x.Message<CancelScheduledMessage>(m => m.UsePartitioner(partitioner, p => p.Message.TokenId)));
                            });
                        });
                    });

                    // Should Only ever register one Service
                    builder.RegisterType<SchedulerService>()
                        .SingleInstance();

                    builder.Register(x => new StdSchedulerFactory().GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult())
                        .SingleInstance();

                    var container = builder.Build();

                    return container.Resolve<SchedulerService>();
                });

                s.WhenStarted((service, control) => service.Start());
                s.WhenStopped((service, control) => service.Stop());
            });

            hostConfigurator.SetDisplayName("MT.Net461.Scheduler");
            hostConfigurator.SetServiceName("MT.Net461.Scheduler");
            hostConfigurator.SetDescription("MT.Net461.Scheduler");
        }

        static void ConfigureSerilog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            // Configure Topshelf Logger
            SerilogLogWriterFactory.Use(Log.Logger);
        }
    }
}
