using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Regen.Data;
using Regen.Models;
using Regen.Services;

var builder = WebApplication.CreateBuilder(args);

// Connect to the database
builder.Services.AddDbContext<RegenContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("RegenContext") ?? throw new InvalidOperationException("Connection string 'RegenContext' not found.")));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register services
builder.Services.AddScoped<GoogleFormService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Create a service scope to get an RegenContext instance using DI and seed the database.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    SeedData.Initialize(services);
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
