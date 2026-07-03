using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSession();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Serve static files (wwwroot)
app.UseStaticFiles();

// Set Default Page
app.MapGet("/", context =>
{
    context.Response.Redirect("/ImPex/Index");
    return Task.CompletedTask;
});

app.UseSession();

app.MapRazorPages();

app.Run();
