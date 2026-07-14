using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.IISIntegration;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});

// Add services to the container
builder.Services.AddRazorPages();

builder.Services.AddSession();

builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ProcurementReminderService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();

builder.Services.AddHostedService<ProcurementReminderHostedService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LiveConnection")));

// Use IIS Windows Authentication
builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware pipeline
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();