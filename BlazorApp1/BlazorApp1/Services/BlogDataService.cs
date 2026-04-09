using BlazorApp1.Models;
using Microsoft.Data.SqlClient;
using System.Threading;

namespace BlazorApp1.Services;

public class BlogDataService
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);
    private static volatile bool isInitialized;

    private readonly IConfiguration configuration;
    private readonly ILogger<BlogDataService> logger;

    public BlogDataService(IConfiguration configuration, ILogger<BlogDataService> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        if (isInitialized)
        {
            return;
        }

        await InitializationLock.WaitAsync();

        try
        {
            if (isInitialized)
            {
                return;
            }

        const string sql = """
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BlogPosts' AND xtype='U')
            BEGIN
                CREATE TABLE BlogPosts (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Title NVARCHAR(200) NOT NULL,
                    Author NVARCHAR(120) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
                )
            END
            """;

            await using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
            isInitialized = true;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    public async Task<List<BlogPost>> GetAllPostsAsync()
    {
        await InitializeDatabaseAsync();

        const string sql = "SELECT Id, Title, Author, Content, CreatedAt FROM BlogPosts ORDER BY Id DESC";
        var posts = new List<BlogPost>();

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            posts.Add(new BlogPost
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Author = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return posts;
    }

    public async Task CreatePostAsync(string title, string author, string content)
    {
        await InitializeDatabaseAsync();

        const string sql = "INSERT INTO BlogPosts (Title, Author, Content) VALUES (@Title, @Author, @Content)";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Title", title);
        command.Parameters.AddWithValue("@Author", author);
        command.Parameters.AddWithValue("@Content", content);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdatePostAsync(int id, string title, string author, string content)
    {
        await InitializeDatabaseAsync();

        const string sql = "UPDATE BlogPosts SET Title = @Title, Author = @Author, Content = @Content WHERE Id = @Id";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Title", title);
        command.Parameters.AddWithValue("@Author", author);
        command.Parameters.AddWithValue("@Content", content);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeletePostAsync(int id)
    {
        await InitializeDatabaseAsync();

        const string sql = "DELETE FROM BlogPosts WHERE Id = @Id";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    private string GetConnectionString()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("SQLAZURECONNSTR_BlogDb"),
            Environment.GetEnvironmentVariable("ConnectionStrings__BlogDb"),
            configuration.GetConnectionString("BlogDb")
        };

        foreach (var candidate in candidates)
        {
            if (IsUsableConnectionString(candidate))
            {
                return candidate!;
            }
        }

        logger.LogWarning("No usable SQL connection string found for BlogDb.");

        throw new InvalidOperationException("No usable SQL connection string found. Set AZURE_SQL_CONNECTION_STRING, SQLAZURECONNSTR_BlogDb, or ConnectionStrings:BlogDb.");
    }

    private static bool IsUsableConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !value.Contains("YOUR_SERVER", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("YOUR_DATABASE", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("YOUR_USER", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("YOUR_PASSWORD", StringComparison.OrdinalIgnoreCase);
    }
}