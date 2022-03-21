using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.QuartzIntegration;
using MassTransit.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Quartz;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace QuartzService;

public class Startup
{
    static bool? _isRunningInContainer;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public static bool IsRunningInContainer =>
        _isRunningInContainer ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.Configure<RabbitMqTransportOptions>(Configuration.GetSection("RabbitMqTransport"));

        var connectionString = Configuration.GetConnectionString("quartz");

        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sql");

        services.AddQuartz(q =>
        {
            q.SchedulerName = "MassTransit-Scheduler";
            q.SchedulerId = "AUTO";

            q.UseMicrosoftDependencyInjectionJobFactory();

            q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

            q.UseTimeZoneConverter();

            q.UsePersistentStore(s =>
            {
                s.UseProperties = true;
                s.RetryInterval = TimeSpan.FromSeconds(15);

                s.UseSqlServer(connectionString);

                s.UseJsonSerializer();

                s.UseClustering(c =>
                {
                    c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    c.CheckinInterval = TimeSpan.FromSeconds(10);
                });
            });
        });

        services.Configure<QuartzEndpointOptions>(Configuration.GetSection("QuartzEndpoint"));

        services.AddSingleton<QuartzEndpointDefinition>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ScheduleMessageConsumer>(typeof(ScheduleMessageConsumerDefinition));
            x.AddConsumer<CancelScheduledMessageConsumer>(typeof(CancelScheduledMessageConsumerDefinition));

            x.UsingRabbitMq((context, cfg) => { cfg.ConfigureEndpoints(context); });
        });

        services.AddQuartzHostedService(options =>
        {
            options.StartDelay = TimeSpan.FromSeconds(5);
            options.WaitForJobsToComplete = true;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = HealthCheckResponseWriter
            });

            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter });

            endpoints.MapControllers();
        });
    }

    public static Task HealthCheckResponseWriter(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(ToJsonString(result));
    }

    static string ToJsonString(HealthReport result)
    {
        var healthResult = new JsonObject
        {
            ["status"] = result.Status.ToString(),
            ["results"] = new JsonObject(result.Entries.Select(entry => new KeyValuePair<string, JsonNode>(entry.Key,
                new JsonObject
                {
                    ["status"] = entry.Value.Status.ToString(),
                    ["description"] = entry.Value.Description,
                    ["data"] = JsonSerializer.SerializeToNode(entry.Value.Data, SystemTextJsonMessageSerializer.Options)
                }))!)
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        return healthResult.ToJsonString(options);
    }
}