﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Playground.AzureServiceBus.Queues;
using Serilog;
using Serilog.Events;

namespace Playground.AzureServiceBus.Cli
{
    public sealed class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .WriteTo.Console()
                    .CreateLogger()
                    .ForContext<Program>();

            try
            {
                var config = GetConfiguration(args);

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateBootstrapLogger().ForContext<Program>();

                Log.Information("Starting host");
                Log.Information(
                    "Running with CLR {CLRVersion} on {OSVersion}",
                    Environment.Version,
                    Environment.OSVersion);

                var builder = Host.CreateDefaultBuilder(args);

                builder.UseSerilog((context, serviceProvider, configuration) => configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(serviceProvider));

                // add application services
                builder.ConfigureServices((ctx, services) =>
                {
                    services.AddOptions<AzureServiceBusSettings>()
                        .Bind(ctx.Configuration.GetSection("AzureServiceBus"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

                    services.AddSingleton(sp =>
                    {
                        var settings = sp.GetRequiredService<IOptions<AzureServiceBusSettings>>();
                        return new ServiceBusClient(settings.Value.ConnectionString);
                    });

                    services.AddSingleton<ISimpleQueueProducer, SimpleQueueProducer>();
                    services.AddHostedService<SimpleQueueConsumer>();

                    services.AddHostedService<ConsoleCommandService>();
                });

                var host = builder.Build();
                host.Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IConfigurationBuilder SetupConfigurationBuilder(IConfigurationBuilder builder, string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            builder.AddJsonFile("appsettings.json", false)
               .AddJsonFile($"appsettings.{environmentName}.json", true)
               .AddCommandLine(args)
               .AddEnvironmentVariables();

            return builder;
        }

        private static IConfiguration GetConfiguration(string[] args)
        {
            var configBuilder = new ConfigurationBuilder();
            return SetupConfigurationBuilder(configBuilder, args).Build();
        }
    }
}