using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using CustomerPortal_MVC_.Models;

namespace CustomerPortal_MVC_.DAL
{
    public class PortalDbContext : DbContext
    {
        public PortalDbContext() : base("name=TajDynamicsDbContext")
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
        }

        public PortalDbContext(string connectionStringName) : base(connectionStringName.Contains("=") ? connectionStringName : "name=" + connectionStringName)
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
        }

        // Entity Sets
        public virtual DbSet<CustOrderTable> CustOrders { get; set; }
        public virtual DbSet<CustTable> Customers { get; set; }
        public virtual DbSet<DirPartyTable> Parties { get; set; }
        public virtual DbSet<UserInfo> Users { get; set; }
        public virtual DbSet<CustSessionLog> SessionLogs { get; set; }
        public virtual DbSet<TankCapacity> TankCapacities { get; set; }
        public virtual DbSet<CpDeposit> Deposits { get; set; }
        public virtual DbSet<SalesTable> SalesOrders { get; set; }
        public virtual DbSet<LubricantOrderHead> LubricantOrderHeads { get; set; }
        public virtual DbSet<LubricantOrderLine> LubricantOrderLines { get; set; }
        public virtual DbSet<LubricantOrderFinal> LubricantOrderFinals { get; set; }
        public virtual DbSet<PersonalInfo> PersonalInfos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Remove default pluralizing table name convention
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Ignore DTO / virtual entities without dedicated database tables
            modelBuilder.Ignore<LubricantBrand>();
            modelBuilder.Ignore<LubricantProduct>();

            // 1. CustTable & DirPartyTable
            modelBuilder.Entity<CustTable>()
                .HasOptional(c => c.PartyEntity)
                .WithMany(p => p.Customers)
                .HasForeignKey(c => c.Party)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CustTable>().Property(c => c.CreditMax).HasPrecision(18, 2);

            // 2. CustOrderTable & CustTable
            modelBuilder.Entity<CustOrderTable>()
                .HasOptional(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.OrderCreatedUser)
                .WillCascadeOnDelete(false);

            // 3. TankCapacity & CustTable
            modelBuilder.Entity<TankCapacity>()
                .HasOptional(t => t.Customer)
                .WithMany(c => c.TankCapacities)
                .HasForeignKey(t => t.CustAccount)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TankCapacity>().Property(t => t.Capacity).HasPrecision(18, 4);

            // 4. CpDeposit & CustTable
            modelBuilder.Entity<CpDeposit>()
                .HasOptional(d => d.Customer)
                .WithMany(c => c.Deposits)
                .HasForeignKey(d => d.AccountCode)
                .WillCascadeOnDelete(false);

            // 5. CustSessionLog & UserInfo
            modelBuilder.Entity<CustSessionLog>()
                .HasOptional(s => s.User)
                .WithMany(u => u.SessionLogs)
                .HasForeignKey(s => s.Username)
                .WillCascadeOnDelete(false);

            // Lubricant tables are mapped directly via attributes without foreign keys
        }
    }
}
