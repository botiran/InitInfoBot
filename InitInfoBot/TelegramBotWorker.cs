using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

public class TelegramBotWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramBotWorker> _logger;

    public TelegramBotWorker()
    {
    }

    public TelegramBotWorker(IServiceProvider serviceProvider, ILogger<TelegramBotWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Bot Worker starting.");

        using var scope = _serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            DropPendingUpdates = true,
        };

        var me = await botClient.GetMe(stoppingToken);
        _logger.LogInformation("Bot started: @{Username}", me.Username);

        await botClient.ReceiveAsync(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );
    }
}
