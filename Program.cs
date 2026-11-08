using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Models;
using Regen.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

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
builder.Services.AddScoped<GoogleFormService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
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
                throw new InvalidOperationException("Google authentication data is missing.");
            }

            if (string.IsNullOrEmpty(googleId) ||
                string.IsNullOrEmpty(email))
            {
                throw new Exception(
                    "Google authentication data is missing.");
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
            var now = DateTime.UtcNow;

            if (user == null)
            {
                user = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    Name = name ?? email,
                    AvatarUrl = avatar,
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastLoginAt = now
                };

                db.Users.Add(user);
            }
            else
            {
                user.Email = email;
                user.Name = name ?? email;
                user.AvatarUrl = avatar;
                user.UpdatedAt = now;
                user.LastLoginAt = now;
            }

            await db.SaveChangesAsync();

            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim(
                    "RegenUserId",
                    user.Id.ToString()
                ));
            }
        };
    });

var app = builder.Build();

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
