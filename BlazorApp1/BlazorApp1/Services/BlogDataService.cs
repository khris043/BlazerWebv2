using BlazorApp1.Models;
using Microsoft.Data.SqlClient;

namespace BlazorApp1.Services;

public class BlogDataService
{
    private readonly IConfiguration configuration;

    public BlogDataService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task InitializeDatabaseAsync()
    {
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
    }

    public async Task<List<BlogPost>> GetAllPostsAsync()
    {
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
        const string sql = "DELETE FROM BlogPosts WHERE Id = @Id";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    private string GetConnectionString()
    {
        var envConnectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            return envConnectionString;
        }

        var configuredConnectionString = configuration.GetConnectionString("BlogDb");
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        throw new InvalidOperationException("No SQL connection string found. Set AZURE_SQL_CONNECTION_STRING or ConnectionStrings:BlogDb.");
    }
}