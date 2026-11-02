using System.Data.Entity;
using DeviceManagement.Models;

namespace DeviceManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        static ApplicationDbContext()
        {
            // Schema is maintained by db.sql; EF Code First must not create or compare it at startup.
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public ApplicationDbContext() : base("DefaultConnection") { }

        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<InspectionRequest> InspectionRequests { get; set; }
        public DbSet<InspectionAsset> InspectionAssets { get; set; }
        public DbSet<InspectionAttachment> InspectionAttachments { get; set; }
        public DbSet<InsuranceAsset> InsuranceAssets { get; set; }
        public DbSet<InsuranceAttachment> InsuranceAttachments { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<InspectionDevice> InspectionDevices { get; set; }
        public DbSet<MaterialCatalogItem> Materials { get; set; }
        public DbSet<StatusCatalogItem> Statuses { get; set; }
        public DbSet<UnitCatalogItem> Units { get; set; }
        public DbSet<ClassificationCatalogItem> Classifications { get; set; }
        public DbSet<ProjectCatalogItem> DepartmentsProjects { get; set; }
        public DbSet<InsuranceTypeCatalogItem> InsuranceTypes { get; set; }
        public DbSet<SupplierCatalogItem> Suppliers { get; set; }
        public DbSet<InspectionTypeCatalogItem> InspectionTypes { get; set; }
        public DbSet<Accident> Accidents { get; set; }
        public DbSet<AccidentCauseCatalogItem> AccidentCauses { get; set; }
        public DbSet<AccidentTypeCatalogItem> AccidentTypes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng nếu cần
            modelBuilder.Entity<Insurance>().ToTable("Insurances");
            modelBuilder.Entity<Insurance>().Property(x => x.Code).IsRequired();
            modelBuilder.Entity<InspectionRequest>().ToTable("InspectionRequests");
            modelBuilder.Entity<InspectionAsset>().ToTable("InspectionAssets");
            modelBuilder.Entity<InspectionAttachment>().ToTable("InspectionAttachments");
            modelBuilder.Entity<InsuranceAsset>().ToTable("InsuranceAssets");
            modelBuilder.Entity<InsuranceAttachment>().ToTable("InsuranceAttachments");
            modelBuilder.Entity<Device>().ToTable("Devices");
            modelBuilder.Entity<InspectionDevice>().ToTable("InspectionDevices");
            modelBuilder.Entity<MaterialCatalogItem>().ToTable("Materials");
            modelBuilder.Entity<StatusCatalogItem>().ToTable("Status");
            modelBuilder.Entity<UnitCatalogItem>().ToTable("Units");
            modelBuilder.Entity<ClassificationCatalogItem>().ToTable("Classifications");
            modelBuilder.Entity<ProjectCatalogItem>().ToTable("DepartmentsProjects");
            modelBuilder.Entity<InsuranceTypeCatalogItem>().ToTable("InsuranceTypes");
            modelBuilder.Entity<SupplierCatalogItem>().ToTable("Suppliers");
            modelBuilder.Entity<InspectionTypeCatalogItem>().ToTable("InspectionTypes");
            modelBuilder.Entity<Accident>().ToTable("Accidents");
            modelBuilder.Entity<AccidentCauseCatalogItem>().ToTable("AccidentCause");
            modelBuilder.Entity<AccidentTypeCatalogItem>().ToTable("AccidentTypes");
        }
    }
}
