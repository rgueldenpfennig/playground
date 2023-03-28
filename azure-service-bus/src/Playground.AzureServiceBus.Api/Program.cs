using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Playground.AzureServiceBus.Queues;
using Serilog;
using Serilog.Events;

namespace Playground.AzureServiceBus.Api;

public class Program
{
    public static async Task<int> Main(string[] args)
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

            Log.Information("Starting web host");
            Log.Information(
                "Running with CLR {CLRVersion} on {OSVersion}",
                Environment.Version,
                Environment.OSVersion);

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((context, serviceProvider, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(serviceProvider));

            // add application services
            builder.Services.AddOptions<AzureServiceBusSettings>()
                .Bind(builder.Configuration.GetSection("AzureServiceBus"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<AzureServiceBusSettings>>();
                return new ServiceBusClient(settings.Value.ConnectionString);
            });

            builder.Services.AddSingleton<ISimpleQueueProducer, SimpleQueueProducer>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // build application and setup middlewares
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseSerilogRequestLogging();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();

            app.MapQueues();

            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Web host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IConfigurationBuilder SetupConfigurationBuilder(IConfigurationBuilder builder, string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

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