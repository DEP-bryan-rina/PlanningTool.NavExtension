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
        public DbSet<ImPexCenterModel> GetImPexItems { get; set; }
        public DbSet<ProcurementCenterModel> GetTaggedItems { get; set; }
        public DbSet<ReminderNotification> tblReminderNotification { get; set; }
        public DbSet<EmailNotication> tblEmailNotification { get; set; }

        public DbSet<ProcurementCenterDto> ProcurementCenterDto { get; set; }
        public DbSet<ProcurementCenterTaggedDto> ProcurementCenterTaggedDto { get; set; }

        public DbSet<EmailNotication> EmailNotifications { get; set; }

        public DbSet<ExpirationMonitoringModel> ExpirationMonitoring { get; set; }

        public DbSet<QADashboardItem> QADashboardItems { get; set; }

        public DbSet<TagProcurementRequest> TagProcurementRequests { get; set; }

        public DbSet<PurchaseLineModel> PurchaseLineList { get; set; }

        public DbSet<ProcurementCenterBalance> ProcurementCenterBalanceView { get; set; }

        public DbSet<ContainerETAStatusViewModel> ContainerETAStatusView { get; set; }

        public DbSet<vw_ProcurementETAEmailNotification> vw_ProcurementETAEmailNotification { get; set; }

        public DbSet<ShipmentEmailHistory> ShipmentEmailHistory { get; set; }

        public DbSet<ShipmentReminderTracking> ShipmentReminderTracking { get; set; }

        public DbSet<QARepairMRPDetail> QARepairMRPDetails { get; set; }

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
            modelBuilder.Entity<ReminderNotification>()
                .ToTable("_tblReminderNotification");
            modelBuilder.Entity<QADashboardItem>()
                .HasNoKey()
                .ToView(null);

            modelBuilder.Entity<PurchaseLineModel>()
                .HasNoKey()
                .ToView(null);

            modelBuilder.Entity<ProcurementCenterBalance>()
                .HasNoKey()
                .ToView("vw_ProcurementCenterBalance");

            modelBuilder.Entity<vw_ProcurementETAEmailNotification>()               
               .ToView("vw_ProcurementETAEmailNotification");

            modelBuilder.Entity<ShipmentEmailHistory>()
                .ToTable("_tblShipmentEmailHistory");

            modelBuilder.Entity<ShipmentReminderTracking>()
                .ToTable("_tblShipmentReminderTracking");


            base.OnModelCreating(modelBuilder);

            modelBuilder
               .Entity<QADashboardItem>()
               .HasNoKey();

            modelBuilder
                .Entity<QARepairMRPDetail>()
                .HasNoKey();
        }
    }
}
