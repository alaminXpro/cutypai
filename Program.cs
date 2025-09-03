using System.Security.Cryptography;
using System.Text;
using cutypai.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace cutypai;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog first
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/cutypai-.txt", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            Env.Load(); // load .env in dev

            var builder = WebApplication.CreateBuilder(args);

            // Use Serilog
            builder.Host.UseSerilog();

            // MVC + API
            builder.Services.AddControllersWithViews();

            // ----- Mongo options with enhanced configuration -----
            builder.Services.Configure<MongoDbSettings>(opts =>
            {
                builder.Configuration.GetSection(MongoDbSettings.SectionName).Bind(opts);
                var envConn = Environment.GetEnvironmentVariable("MongoDbSettings__ConnectionString")
                              ?? Environment.GetEnvironmentVariable("mongodb");
                if (!string.IsNullOrWhiteSpace(envConn)) opts.ConnectionString = envConn;
            });

            // Enhanced Mongo services with connection pooling and timeouts
            builder.Services.AddSingleton<IMongoClient>(sp =>
            {
                var s = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                if (string.IsNullOrWhiteSpace(s.ConnectionString))
                    throw new InvalidOperationException("MongoDB connection string is not configured.");

                var settings = MongoClientSettings.FromConnectionString(s.ConnectionString);
                settings.MaxConnectionPoolSize = 100;
                settings.MinConnectionPoolSize = 5;
                settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(5);
                settings.ConnectTimeout = TimeSpan.FromSeconds(30);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
                settings.SocketTimeout = TimeSpan.FromSeconds(30);

                return new MongoClient(settings);
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
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            builder.Services.AddScoped<IPasswordValidationService, PasswordValidationService>();

            // ----- Enhanced JWT options -----
            builder.Services.Configure<JwtSettings>(opts =>
            {
                builder.Configuration.GetSection(JwtSettings.SectionName).Bind(opts);

                // Allow overriding key from env
                var envKey = Environment.GetEnvironmentVariable("Jwt__Key");
                if (!string.IsNullOrWhiteSpace(envKey)) opts.Key = envKey;

                // Generate secure key if not provided
                if (string.IsNullOrWhiteSpace(opts.Key))
                {
                    Log.Warning("No JWT key found in configuration. Generating a secure key for this session.");
                    opts.Key = GenerateSecureJwtKey();
                }

                // Validate key security
                if (!opts.IsKeySecure())
                    throw new InvalidOperationException("JWT Key must be at least 32 characters long for security.");

                // Sensible defaults with shorter access token lifetime
                if (opts.AccessTokenMinutes <= 0) opts.AccessTokenMinutes = 15;
                if (opts.RefreshTokenDays <= 0) opts.RefreshTokenDays = 7;
            });

            builder.Services.AddScoped<ITokenService, TokenService>();

            // Enhanced AuthN/Z
            var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
            var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? jwt.Key;
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                Log.Warning("JWT Key is missing or too short. Using generated key for this session.");
                jwtKey = GenerateSecureJwtKey();
            }

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
                        ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
                    };
                });

            builder.Services.AddAuthorization();

            // Enhanced CORS for production readiness
            builder.Services.AddCors(o =>
            {
                o.AddPolicy("Default", p =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        // Allow any origin in development
                        p.AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .SetIsOriginAllowed(_ => true);
                    }
                    else
                    {
                        // Restrict origins in production - update with your actual domains
                        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                                             ?? new[] { "https://yourdomain.com" };

                        p.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
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

            // Health & indexes with proper logging
            await PingMongoAsync(app.Services);
            await EnsureMongoIndexesAsync(app.Services);

            Log.Information("Application starting...");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task PingMongoAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        try
        {
            await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            Log.Information("✅ MongoDB ping OK.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ MongoDB ping failed: {Message}", ex.Message);
        }
    }

    private static async Task EnsureMongoIndexesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

        try
        {
            // Users collection indexes
            var users = db.GetCollection<User>("users");
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

            // Refresh tokens collection indexes
            var refreshTokens = db.GetCollection<RefreshToken>("refresh_tokens");
            var tokenKeys = Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token);
            var tokenIndex = new CreateIndexModel<RefreshToken>(
                tokenKeys,
                new CreateIndexOptions
                {
                    Name = "ux_token",
                    Unique = true
                });
            await refreshTokens.Indexes.CreateOneAsync(tokenIndex);

            // Index for cleanup of expired tokens
            var expiryKeys = Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.ExpiresAtUtc);
            var expiryIndex = new CreateIndexModel<RefreshToken>(
                expiryKeys,
                new CreateIndexOptions
                {
                    Name = "ix_expires_at",
                    ExpireAfter = TimeSpan.FromDays(30) // Auto-delete after 30 days
                });
            await refreshTokens.Indexes.CreateOneAsync(expiryIndex);

            Log.Information("✅ MongoDB indexes created successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Failed to create MongoDB indexes: {Message}", ex.Message);
        }
    }

    private static string GenerateSecureJwtKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[64]; // 512 bits
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }
}