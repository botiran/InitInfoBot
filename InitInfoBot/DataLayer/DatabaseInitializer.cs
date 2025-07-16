using Dapper;
using Microsoft.Extensions.Logging;

public class DatabaseInitializer
{
    private readonly IDatabaseService _dbService;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDatabaseService dbService, ILogger<DatabaseInitializer> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");
            using var connection = _dbService.CreateConnection();

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id BIGINT PRIMARY KEY,
                    FirstName TEXT NOT NULL,
                    LastName TEXT,
                    Username TEXT,
                    StartDate TEXT NOT NULL
                );");

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Chats (
                    Id BIGINT PRIMARY KEY,
                    Title TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Username TEXT,
                    AddedByUserId BIGINT NOT NULL,
                    DateAdded TEXT NOT NULL,
                    FOREIGN KEY (AddedByUserId) REFERENCES Users(Id)
                );");

            _logger.LogInformation("Database initialization complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database initialization.");
            throw;
        }
    }
}
