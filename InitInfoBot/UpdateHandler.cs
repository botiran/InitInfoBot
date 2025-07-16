using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IDatabaseService _dbService;
    private readonly BotConfiguration _botConfig;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, IDatabaseService dbService, BotConfiguration botConfig)
    {
        _botClient = botClient;
        _logger = logger;
        _dbService = dbService;
        _botConfig = botConfig;
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };
        _logger.LogError("Polling Error: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => OnMessageReceived(update.Message!),
            UpdateType.MyChatMember => OnMyChatMember(update.MyChatMember!),
            _ => Task.CompletedTask // Silently ignore other update types
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandlePollingErrorAsync(botClient, exception, cancellationToken);
        }
    }

    private async Task OnMessageReceived(Message message)
    {
        if (message.From is null || message.Text is null) return;

        var user = message.From;
        var command = message.Text.Split(' ')[0].ToLowerInvariant();

        Task action = command switch
        {
            "/start" => ProcessStartCommand(user),
            "/help" => ProcessHelpCommand(user),
            "/getme" => ProcessGetMeCommand(user),
            "/removeall" => ProcessRemoveAllCommand(user),
            "/updateall" => ProcessUpdateAllCommand(user),
            _ => ProcessUnknownCommand(user)
        };
        await action;
    }

    private async Task ProcessStartCommand(User from)
    {
        await _dbService.AddOrUpdateUserAsync(new BotUser
        {
            Id = from.Id,
            FirstName = from.FirstName,
            LastName = from.LastName,
            Username = from.Username,
            StartDate = DateTime.UtcNow
        });
        await ProcessHelpCommand(from);
    }

    private async Task ProcessHelpCommand(User from)
    {
        var botName = _botConfig.BotName;
        var helpText = new StringBuilder();
        helpText.AppendLine($"🤖 Welcome to **{botName}**!");
        helpText.AppendLine();
        helpText.AppendLine("This bot helps you easily get information about the groups and channels where you are an admin.");
        helpText.AppendLine();
        helpText.AppendLine("Available commands:");
        helpText.AppendLine("`/start` - Displays the welcome message and guide.");
        helpText.AppendLine("`/help` - Shows this help message.");
        helpText.AppendLine("`/getme` - Displays your Telegram user information.");
        helpText.AppendLine("`/updateall` - Fetches an updated report for all your chats.");
        helpText.AppendLine("`/removeall` - Makes the bot leave all chats you've added it to.");

        await _botClient.SendMessage(chatId: from.Id, text: helpText.ToString(), parseMode: ParseMode.Markdown);
    }

    private async Task ProcessGetMeCommand(User from)
    {
        var userText = new StringBuilder();
        userText.AppendLine("📄 **Your User Information:**");
        userText.AppendLine($"**ID:** `{from.Id}`");
        userText.AppendLine($"**First Name:** `{from.FirstName}`");
        if (!string.IsNullOrEmpty(from.LastName))
            userText.AppendLine($"**Last Name:** `{from.LastName}`");
        if (!string.IsNullOrEmpty(from.Username))
            userText.AppendLine($"**Username:** `@{from.Username}`");

        await _botClient.SendMessage(chatId: from.Id, text: userText.ToString(), parseMode: ParseMode.Markdown);
    }

    private async Task ProcessRemoveAllCommand(User from)
    {
        await _botClient.SendMessage(chatId: from.Id, text: "Processing your request... Please wait.");
        var chats = await _dbService.GetChatsByUserIdAsync(from.Id);
        int successCount = 0;
        int errorCount = 0;

        foreach (var chat in chats)
        {
            try
            {
                await _botClient.LeaveChat(chat.Id);
                await _dbService.RemoveChatAsync(chat.Id);
                successCount++;
                await Task.Delay(300); // Add a small delay to avoid hitting API rate limits
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to leave chat {ChatId} for user {UserId}", chat.Id, from.Id);
            }
        }

        await _botClient.SendMessage(chatId: from.Id, text: $"Operation complete. ✅\nSuccessfully left: {successCount} chats\nFailed to leave: {errorCount} chats");
    }

    private async Task ProcessUpdateAllCommand(User from)
    {
        await _botClient.SendMessage(chatId: from.Id, text: "Fetching new reports... This might take a moment.");
        var chats = await _dbService.GetChatsByUserIdAsync(from.Id);

        if (!chats.Any())
        {
            await _botClient.SendMessage(chatId: from.Id, text: "You haven't added the bot to any chats yet.");
            return;
        }

        foreach (var chat in chats)
        {
            await SendDetailedChatInfo(from.Id, chat.Id);
            await Task.Delay(300); // Add a small delay to avoid hitting API rate limits
        }

        await _botClient.SendMessage(chatId: from.Id, text: "All chat reports have been sent successfully. ✅");
    }

    private async Task ProcessUnknownCommand(User from)
    {
        await _botClient.SendMessage(chatId: from.Id, text: "Invalid command. 🤷‍♂️\nUse `/help` to see the list of available commands.");
    }

    private async Task OnMyChatMember(ChatMemberUpdated myChatMember)
    {
        var userWhoAddedBot = myChatMember.From;
        var chat = myChatMember.Chat;
        var newStatus = myChatMember.NewChatMember.Status;

        // The bot should only react when it's added to a chat or promoted.
        if (newStatus == ChatMemberStatus.Member || newStatus == ChatMemberStatus.Administrator)
        {
            await _dbService.AddOrUpdateUserAsync(new BotUser
            {
                Id = userWhoAddedBot.Id,
                FirstName = userWhoAddedBot.FirstName,
                LastName = userWhoAddedBot.LastName,
                Username = userWhoAddedBot.Username,
                StartDate = DateTime.UtcNow
            });

            await _dbService.AddOrUpdateChatAsync(new ChatInfo
            {
                Id = chat.Id,
                Title = chat.Title ?? "N/A",
                Type = chat.Type.ToString(),
                Username = chat.Username,
                AddedByUserId = userWhoAddedBot.Id,
                DateAdded = DateTime.UtcNow
            });

            await SendDetailedChatInfo(userWhoAddedBot.Id, chat.Id);
        }
    }

    private async Task SendDetailedChatInfo(long userId, long chatId)
    {
        try
        {
            var chat = await _botClient.GetChat(chatId);
            var memberCount = await _botClient.GetChatMemberCount(chatId);

            var sb = new StringBuilder();
            sb.AppendLine($"Chat Report: **{chat.Title}**");
            sb.AppendLine("-----------------------------------");
            sb.AppendLine($"**ID:** `{chat.Id}`");
            sb.AppendLine($"**Members:** `{memberCount}`");

            if (chat.Type != ChatType.Private)
            {
                try
                {
                    var inviteLink = await _botClient.ExportChatInviteLink(chatId);
                    sb.AppendLine($"**Invite Link:** {inviteLink}");
                }
                catch
                {
                    // Silently ignore if the bot cannot create an invite link (e.g., no admin rights).
                }
            }

            await _botClient.SendMessage(chatId: userId, text: sb.ToString(), parseMode: ParseMode.Markdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send detailed info for chat {ChatId} to user {UserId}", chatId, userId);
            await _botClient.SendMessage(chatId: userId, text: $"❌ Failed to retrieve information for chat `{chatId}`.");
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An error occurred from source {Source}", source);
        return Task.CompletedTask;
    }
}
