﻿using Serilog;
using Serilog.Events;

namespace QuartzService;

class Program
{
    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        var host = CreateHostBuilder(args).Build();

        await host.RunAsync();
    }

    static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(builder => builder.AddEnvironmentVariables())
            .UseSerilog()
            .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());
    }
}