using System.Data.Entity;
using DeviceManagement.Models;

namespace DeviceManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection") { }

        public DbSet<Request> Requests { get; set; }
        public DbSet<RequestAsset> RequestAssets { get; set; }
        public DbSet<RequestAttachment> RequestAttachments { get; set; }
        public DbSet<Device> Devices { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng nếu cần
            modelBuilder.Entity<Request>().ToTable("Requests");
            modelBuilder.Entity<Request>().Property(x => x.Code).IsRequired();
            modelBuilder.Entity<RequestAsset>().ToTable("RequestAssets");
            modelBuilder.Entity<RequestAttachment>().ToTable("RequestAttachments");
            modelBuilder.Entity<Device>().ToTable("Devices");
        }
    }
}
