using cutypai.Models;
using DotNetEnv;
using MongoDB.Bson;
using MongoDB.Driver;

namespace cutypai;

public class Program
{
    public static void Main(string[] args)
    {
        // Load environment variables from .env file
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // Get MongoDB connection string from environment variable
        var mongoConnectionString = Environment.GetEnvironmentVariable("mongodb") ?? "";
        var mongoDbSettingsSection = builder.Configuration.GetSection("MongoDbSettings");
        var mongoSettings = mongoDbSettingsSection.Get<MongoDbSettings>() ?? new MongoDbSettings();
        mongoSettings.ConnectionString = mongoConnectionString;
        builder.Services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = mongoConnectionString;
            options.DatabaseName = mongoSettings.DatabaseName;
        });
        builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));

        // Test MongoDB connection at startup
        try
        {
            var client = new MongoClient(mongoConnectionString);
            var database = client.GetDatabase(mongoSettings.DatabaseName);
            database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1)).Wait();
            Console.WriteLine($"MongoDB connection to '{mongoSettings.DatabaseName}' succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MongoDB connection failed: {ex.Message}");
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllerRoute(
                "default",
                "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();
    }
}