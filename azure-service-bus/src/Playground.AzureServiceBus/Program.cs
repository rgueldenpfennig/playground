using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog.Events;
using Serilog;

namespace Playground.AzureServiceBus;

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

            var builder = Host.CreateDefaultBuilder(args)
                              .ConfigureAppConfiguration(builder => SetupConfigurationBuilder(builder, args));

            builder.UseSerilog((context, serviceProvider, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(serviceProvider));

            // add application services
            // builder.ConfigureServices

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
        var environmentName = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

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