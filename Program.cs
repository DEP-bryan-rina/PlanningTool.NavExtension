using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Data;
using PlanningTool.NavExtension.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;

var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSession();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();


builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization();


var app = builder.Build();


app.UseAuthentication();
app.UseAuthorization();


// Serve static files (wwwroot)
app.UseStaticFiles();


app.UseSession();

app.MapRazorPages();

app.Run();
