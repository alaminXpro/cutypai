using System.Text;
using cutypai.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

namespace cutypai;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load(); // load .env in dev

        var builder = WebApplication.CreateBuilder(args);

        // MVC + API
        builder.Services.AddControllersWithViews();

        // ----- Mongo options -----
        builder.Services.Configure<MongoDbSettings>(opts =>
        {
            builder.Configuration.GetSection(MongoDbSettings.SectionName).Bind(opts);
            var envConn = Environment.GetEnvironmentVariable("MongoDbSettings__ConnectionString")
                          ?? Environment.GetEnvironmentVariable("mongodb");
            if (!string.IsNullOrWhiteSpace(envConn)) opts.ConnectionString = envConn;
        });

        // Mongo services
        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var s = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            if (string.IsNullOrWhiteSpace(s.ConnectionString))
                throw new InvalidOperationException("MongoDB connection string is not configured.");
            return new MongoClient(s.ConnectionString);
        });
        builder.Services.AddScoped<IMongoDatabase>(sp =>
        {
            var s = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            if (string.IsNullOrWhiteSpace(s.DatabaseName))
                throw new InvalidOperationException("MongoDB DatabaseName is not configured.");
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(s.DatabaseName);
        });
        builder.Services.AddScoped<MongoCollectionFactory>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();

        // ----- JWT options -----
        builder.Services.Configure<JwtSettings>(opts =>
        {
            builder.Configuration.GetSection(JwtSettings.SectionName).Bind(opts);

            // Allow overriding key from env
            var envKey = Environment.GetEnvironmentVariable("Jwt__Key");
            if (!string.IsNullOrWhiteSpace(envKey)) opts.Key = envKey;

            // sensible default lifetime
            if (opts.AccessTokenMinutes <= 0) opts.AccessTokenMinutes = 60;
        });

        builder.Services.AddScoped<ITokenService, TokenService>();

        // AuthN/Z
        var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? jwt.Key;
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT Key is not configured.");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        builder.Services.AddAuthorization();

        // (Optional) CORS for API clients (adjust origin)
        builder.Services.AddCors(o =>
        {
            o.AddPolicy("Default", p => p
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true)); // replace with precise origins in prod
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseCors("Default");

        app.UseAuthentication(); // <-- JWT
        app.UseAuthorization();

        app.MapStaticAssets();

        // MVC routes
        app.MapControllerRoute(
                "default",
                "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        // Health & indexes
        await PingMongoAsync(app.Services);
        await EnsureMongoIndexesAsync(app.Services);

        app.Run();
    }

    private static async Task PingMongoAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        try
        {
            await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine("✅ MongoDB ping OK.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MongoDB ping failed: {ex.Message}");
        }
    }

    private static async Task EnsureMongoIndexesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var users = db.GetCollection<User>("users");

        // Unique email (case-insensitive)
        var emailKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        var emailIndex = new CreateIndexModel<User>(
            emailKeys,
            new CreateIndexOptions
            {
                Name = "ux_email_ci",
                Unique = true,
                Collation = new Collation("en", strength: CollationStrength.Secondary)
            });
        await users.Indexes.CreateOneAsync(emailIndex);
    }
}