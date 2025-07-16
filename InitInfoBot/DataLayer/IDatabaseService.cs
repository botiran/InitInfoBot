using Microsoft.Data.Sqlite;

public interface IDatabaseService
{
    SqliteConnection CreateConnection();
    Task AddOrUpdateUserAsync(BotUser user);
    Task AddOrUpdateChatAsync(ChatInfo chat);
    Task<IEnumerable<ChatInfo>> GetChatsByUserIdAsync(long userId);
    Task RemoveChatAsync(long chatId);
}
