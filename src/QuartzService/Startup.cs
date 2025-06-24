using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;

namespace QuartzService;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.Configure<RabbitMqTransportOptions>(Configuration.GetSection("RabbitMqTransport"));

        var connectionString = Configuration.GetConnectionString("quartz")
            ?? throw new InvalidOperationException("Connection string 'quartz' is not configured.");

        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sql");

        services.AddQuartz(q =>
        {
            q.SchedulerName = "MassTransit-Scheduler";
            q.SchedulerId = "AUTO";

            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 10;
            });

            q.UsePersistentStore(s =>
            {
                s.UseProperties = true;
                s.RetryInterval = TimeSpan.FromSeconds(15);

                s.UseSqlServer(connectionString);

                s.UseClustering(c =>
                {
                    c.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    c.CheckinInterval = TimeSpan.FromSeconds(10);
                });

                var serializerType = Configuration["quartz:serializer:type"]
                    ?? throw new InvalidOperationException("Missing Quartz serializer type configuration.");

                s.SetProperty("quartz.serializer.type", serializerType);
            });
        });

        services.AddMassTransit(x =>
        {
            x.AddPublishMessageScheduler();

            x.AddQuartzConsumers();

            x.AddConsumer<SampleConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.UsePublishMessageScheduler();

                cfg.ConfigureEndpoints(context);
            });
        });

        services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
        });

        services.AddQuartzHostedService(options =>
        {
            options.StartDelay = TimeSpan.FromSeconds(5);
            options.WaitForJobsToComplete = true;
        });

        services.AddHostedService<SuperWorker>();
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
        return context.Response.WriteAsync(result.ToJsonString());
    }
}