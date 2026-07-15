using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Services;

var builder = WebApplication.CreateBuilder(args);

// Kestrel
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(
        context.Configuration.GetSection("Kestrel"));
});

// MVC / Razor Pages
builder.Services.AddControllers();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Account/Login");
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LiveConnection")));

// Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Windows Authentication
builder.Services
    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ProcurementReminderService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();

// Hosted Services
builder.Services.AddHostedService<ProcurementReminderHostedService>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Account/Login");
});

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapRazorPages()
   .RequireAuthorization();

app.MapGet("/", () =>
    Results.Redirect("/Account/Login"));

app.Run();