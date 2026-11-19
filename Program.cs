using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Models;
using Regen.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Regen.Models.Enums;
using OpenAI.Responses;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Connect to the database
builder.Services.AddDbContext<RegenContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RegenContext") ?? throw new InvalidOperationException("Connection string 'RegenContext' not found.")));

builder.Services.AddHttpClient("NoRedirect", c =>
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = System.Net.DecompressionMethods.All
    });

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register services
#pragma warning disable OPENAI001

var apiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException(
        "OpenAI API key is missing."
    );

builder.Services.AddSingleton(
    new ResponsesClient(apiKey)
);

builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<GoogleFormService>();
builder.Services.AddScoped<CreditService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register the background service
builder.Services.AddHostedService<ScheduleWorker>();
builder.Services.AddHostedService<CreditRefundWorker>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ClaimActions.MapJsonKey("picture", "picture");
        options.Events.OnCreatingTicket = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<RegenContext>();
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
                builder.Configuration["Security:CreditClaimKey"]
                ?? throw new InvalidOperationException(
                    "Credit claim key is missing."));

            var googleIdHash = Convert.ToHexString(
                HMACSHA256.HashData(
                    key,
                    Encoding.UTF8.GetBytes(googleId)
                )
            );

            var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RegenContext>();
    var adminEmail = builder.Configuration["Admin:Email"];

    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var admin = await db.Users.FirstOrDefaultAsync(
            u => u.Email == adminEmail
        );

        if (admin != null && admin.Role != UserRole.Admin)
        {
            admin.Role = UserRole.Admin;
            await db.SaveChangesAsync();
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}"); // <-- Update in AspnetCoreMvcStarter

app.Run();
