using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(BotConfiguration config, ILogger<DatabaseService> logger)
    {
        _connectionString = config.ConnectionString;
        _logger = logger;
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

    public async Task AddOrUpdateUserAsync(BotUser user)
    {
        try
        {
            using var connection = CreateConnection();
            var sql = @"
                INSERT INTO Users (Id, FirstName, LastName, Username, StartDate)
                VALUES (@Id, @FirstName, @LastName, @Username, @StartDate)
                ON CONFLICT(Id) DO UPDATE SET
                    FirstName = excluded.FirstName,
                    LastName = excluded.LastName,
                    Username = excluded.Username;";
            await connection.ExecuteAsync(sql, user);
            _logger.LogInformation("User {UserId} ({Username}) saved to database.", user.Id, user.Username ?? "N/A");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user {UserId}.", user.Id);
        }
    }

    public async Task AddOrUpdateChatAsync(ChatInfo chat)
    {
        try
        {
            using var connection = CreateConnection();
            var sql = @"
                INSERT INTO Chats (Id, Title, Type, Username, AddedByUserId, DateAdded)
                VALUES (@Id, @Title, @Type, @Username, @AddedByUserId, @DateAdded)
                ON CONFLICT(Id) DO UPDATE SET
                    Title = excluded.Title,
                    Username = excluded.Username,
                    Type = excluded.Type;";
            await connection.ExecuteAsync(sql, chat);
            _logger.LogInformation("Chat {ChatId} ({ChatTitle}) saved to database.", chat.Id, chat.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save chat {ChatId}.", chat.Id);
        }
    }


    public async Task<IEnumerable<ChatInfo>> GetChatsByUserIdAsync(long userId)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<ChatInfo>(
            "SELECT * FROM Chats WHERE AddedByUserId = @UserId",
            new { UserId = userId }
        );
    }

    public async Task RemoveChatAsync(long chatId)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM Chats WHERE Id = @ChatId",
            new { ChatId = chatId }
        );
    }
}
