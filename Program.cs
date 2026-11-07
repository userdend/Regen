using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Services;

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
