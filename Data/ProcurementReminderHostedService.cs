using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlanningTool.NavExtension.Services;

namespace PlanningTool.NavExtension.Data
{
    public class ProcurementReminderHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public ProcurementReminderHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                var nextRun = now.Date.AddHours(8); // Today 8:00 AM

                if (now >= nextRun)
                {
                    nextRun = nextRun.AddDays(1); // Tomorrow 8:00 AM
                }

                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();

                var reminderService =
                    scope.ServiceProvider.GetRequiredService<ProcurementReminderService>();

                await reminderService.ProcessProcurementReminders();
            }
        }
    }
}
