// ============================================================================
//  Project:    InitInfo Telegram Bot
//  Author:     Behzad Sadeghi
//  GitHub:     https://github.com/botiran
//  Version:    2.3.0
//  Description: A Telegram bot to fetch and display chat information, based on the user's working version.
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting InitInfo Bot Host...");
            var host = CreateHostBuilder(args).Build();

            var dbInitializer = host.Services.GetRequiredService<DatabaseInitializer>();
            await dbInitializer.InitializeAsync();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly.");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
       Host.CreateDefaultBuilder(args)
           .ConfigureAppConfiguration((context, config) =>
           {
               config.SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
           })
           .UseSerilog()
           .ConfigureServices((hostContext, services) =>
           {
               var botConfig = hostContext.Configuration.GetSection("BotConfiguration").Get<BotConfiguration>() ?? new BotConfiguration();
               services.AddSingleton(botConfig);

               services.AddSingleton<IDatabaseService, DatabaseService>();
               services.AddSingleton<DatabaseInitializer>();

               
               services.AddHttpClient("telegram_bot_client")
                       .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                       {
                           var config = sp.GetRequiredService<BotConfiguration>();
                           var options = new TelegramBotClientOptions(
                               token: config.BotToken,
                               baseUrl: string.IsNullOrEmpty(config.TelegramBotApiServer) ? null : config.TelegramBotApiServer
                           );
                           return new TelegramBotClient(options, httpClient);
                       });
               

               services.AddScoped<IUpdateHandler, UpdateHandler>();
               services.AddHostedService<TelegramBotWorker>();
           });
}
