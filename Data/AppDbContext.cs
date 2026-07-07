using Microsoft.EntityFrameworkCore;
using PlanningTool.NavExtension.Models;

namespace PlanningTool.NavExtension.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<UserModel> tblUserCenter { get; set; }
        public DbSet<ImPexCenterModel> tblImPexCenter { get; set; }
        public DbSet<ProcurementCenterModel> tblProcurementCenter { get; set; }
        public DbSet<ReminderNofication> tblReminderNotification { get; set; }

        public DbSet<ProcurementCenterDto> ProcurementCenterDto { get; set; }

        public DbSet<EmailNotication> EmailNotifications { get; set; }

        public DbSet<ExpirationMonitoringModel> ExpirationMonitoring { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModel>()
                .ToTable("_tblUsers");
            modelBuilder.Entity<ImPexCenterModel>()
                .ToTable("_tblImPexCenter");
            modelBuilder.Entity<ProcurementCenterModel>()
                .ToTable("_tblProcurementCenter");
            modelBuilder.Entity<EmailNotication>()
                .ToTable("_tblEmailNotification");
            modelBuilder.Entity<ReminderNofication>()
                .ToTable("_tblReminderNotification");
        }
    }
}
