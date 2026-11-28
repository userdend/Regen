using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenAI.Responses;
using Regen.Models.Enums;
using Regen.Data;
using Regen.Models;
using Regen.Services;
using Serilog;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Regen")
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/regen-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            rollOnFileSizeLimit: true,
            shared: true
        )
        .WriteTo.Seq("http://seq:80");
});

var configuration = builder.Configuration;
var services = builder.Services;

// Configuration and external clients
var stripeSecretKey = configuration["Stripe:SecretKey"]
    ?? throw new InvalidOperationException("Stripe secret key missing.");

_ = configuration["Stripe:WebhookSecret"]
    ?? throw new InvalidOperationException("Stripe webhook secret missing.");

StripeConfiguration.ApiKey = stripeSecretKey;

var connectionString = configuration.GetConnectionString("RegenContext")
    ?? throw new InvalidOperationException(
        "Connection string 'RegenContext' not found.");

services.AddDbContext<RegenContext>(options =>
    options.UseNpgsql(connectionString));

services.AddHttpClient("NoRedirect", client =>
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = System.Net.DecompressionMethods.All
    });

services.AddHttpClient("Scheduler", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.MaxResponseContentBufferSize = 50 * 1024 * 1024;
});

services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Application services
#pragma warning disable OPENAI001

var openAiApiKey = configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException(
        "OpenAI API key is missing."
    );

services
    .AddHealthChecks()
    .AddCheck<WorkerHealthCheck>(
        "workers",
        tags: ["ready", "workers"])
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        tags: ["ready", "database"]);

services.AddSingleton<WorkerHealthTracker>();
services.AddSingleton(new ResponsesClient(openAiApiKey));

services.AddScoped<NotificationService>();
services.AddScoped<OpenAIService>();
services.AddScoped<GoogleFormService>();
services.AddScoped<CreditService>();
services.AddScoped<AccountDeletionService>();
services.AddScoped<PaymentService>();

services.AddControllersWithViews();

services.AddHostedService<ScheduleWorker>();
services.AddHostedService<CreditRefundWorker>();

// Authentication
services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ClaimActions.MapJsonKey("picture", "picture");
        options.Events.OnCreatingTicket = async context =>
        {
            var db = context.HttpContext.RequestServices
                .GetRequiredService<RegenContext>();
            var googleId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var name = context.Principal?.FindFirstValue(ClaimTypes.Name);
            var avatar = context.Principal?.FindFirstValue("picture");

            if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException(
                    "Google authentication data is missing.");
            }

            // Generate a stable, keyed identifier.
            var key = Convert.FromBase64String(
                configuration["Security:CreditClaimKey"]
                ?? throw new InvalidOperationException(
                    "Credit claim key is missing."));

            var googleIdHash = Convert.ToHexString(
                HMACSHA256.HashData(
                    key,
                    Encoding.UTF8.GetBytes(googleId)
                )
            );

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
            var now = DateTime.UtcNow;

            if (user == null)
            {
                const int freeCredits = 30;

                await using var transaction =
                    await db.Database.BeginTransactionAsync();

                // Atomic claim prevents duplicate promotional grants.
                var claimed = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "FreeCreditClaims"
                        ("Id", "GoogleIdHash", "ClaimedAt")
                    VALUES
                        ({Guid.NewGuid()}, {googleIdHash}, {now})
                    ON CONFLICT ("GoogleIdHash") DO NOTHING
                    """);

                user = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    Name = name ?? email,
                    AvatarUrl = avatar,
                    Credit = claimed > 0 ? freeCredits : 0,
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastLoginAt = now
                };

                db.Users.Add(user);

                await db.SaveChangesAsync();

                if (claimed > 0)
                {
                    db.CreditTransactions.Add(new CreditTransaction
                    {
                        UserId = user.Id,
                        Amount = freeCredits,
                        Type = "promotion",
                        OperationKey = $"welcome:{googleIdHash}"
                    });

                    await db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            else
            {
                user.Email = email;
                user.Name = name ?? email;
                user.AvatarUrl = avatar;
                user.UpdatedAt = now;
                user.LastLoginAt = now;

                await db.SaveChangesAsync();
            }

            var adminGoogleId = configuration["Admin:GoogleId"];

            if (!string.IsNullOrWhiteSpace(adminGoogleId) &&
                googleId == adminGoogleId &&
                user.Role != UserRole.Admin)
            {
                user.Role = UserRole.Admin;
                await db.SaveChangesAsync();
            }

            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim(
                    "RegenUserId",
                    user.Id.ToString()
                ));

                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString()
                ));
            }
        };
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/database", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database")
});

app.MapHealthChecks("/health/workers", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("workers")
});

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

app.Run();
