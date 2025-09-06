using ERP.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // User & Company Management
    public DbSet<Company> Company { get; set; }
    public DbSet<Role> Role { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<Module> Module { get; set; }
    public DbSet<Component> Component { get; set; }
    public DbSet<Permission> Permission { get; set; }

    // Masters
    public DbSet<Category> Category { get; set; }
    public DbSet<SubCategory> SubCategory { get; set; }
    public DbSet<Brand> Brand { get; set; }
    public DbSet<UOM> UOM { get; set; }
    public DbSet<Item> Item { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Vender> Vender { get; set; }
    public DbSet<Transporter> Transporter { get; set; }
    public DbSet<Warehouse> Warehouse { get; set; }

    // Accounts
    public DbSet<PaymentVoucher> PaymentVoucher { get; set; }
    public DbSet<Bank> Bank { get; set; }
    public DbSet<AccountType> AccountType { get; set; }
    public DbSet<ChartOfAccount> ChartOfAccount { get; set; }
    public DbSet<JournalEntry> JournalEntry { get; set; }
    public DbSet<JournalDetail> JournalDetail { get; set; }
    public DbSet<Ledger> Ledger { get; set; }

    // Inventory
    public DbSet<StockMaster> StockMaster { get; set; }
    public DbSet<StockDetail> StockDetail { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Decimal precision adjustments
        modelBuilder.Entity<PaymentVoucher>()
            .Property(p => p.amount).HasPrecision(18, 2);

        modelBuilder.Entity<Bank>()
            .Property(p => p.opening_balance).HasPrecision(18, 2);

        modelBuilder.Entity<Customer>()
            .Property(c => c.credit_limit).HasPrecision(18, 2);

        modelBuilder.Entity<Customer>()
            .Property(c => c.current_balance).HasPrecision(18, 2);

        modelBuilder.Entity<Vender>()
            .Property(v => v.current_balance).HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.purchase_rate).HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.sale_rate).HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.rate).HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.discount_amount).HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.total_amount).HasPrecision(18, 2);

        modelBuilder.Entity<StockMaster>()
            .Property(s => s.total_amount).HasPrecision(18, 2);

        modelBuilder.Entity<StockMaster>()
            .Property(s => s.discount_amount).HasPrecision(18, 2);

        modelBuilder.Entity<StockMaster>()
            .Property(s => s.tax_amount).HasPrecision(18, 2);

        modelBuilder.Entity<StockMaster>()
            .Property(s => s.net_amount).HasPrecision(18, 2);

        modelBuilder.Entity<StockDetail>()
            .Property(s => s.rate).HasPrecision(18, 2);

        modelBuilder.Entity<StockDetail>()
            .Property(s => s.amount).HasPrecision(18, 2);
        modelBuilder.Entity<StockDetail>()
           .Property(s => s.discount_percentage).HasPrecision(18, 2);

        modelBuilder.Entity<StockDetail>()
            .Property(s => s.discount_amount).HasPrecision(18, 2);

        modelBuilder.Entity<StockDetail>()
            .Property(s => s.net_amount).HasPrecision(18, 2);

        modelBuilder.Entity<JournalEntry>()
            .Property(j => j.total_debit).HasPrecision(18, 2);

        modelBuilder.Entity<JournalEntry>()
            .Property(j => j.total_credit).HasPrecision(18, 2);

        modelBuilder.Entity<JournalDetail>()
            .Property(j => j.debit_amount).HasPrecision(18, 2);

        modelBuilder.Entity<JournalDetail>()
            .Property(j => j.credit_amount).HasPrecision(18, 2);

        modelBuilder.Entity<Ledger>()
            .Property(l => l.debit_amount).HasPrecision(18, 2);

        modelBuilder.Entity<Ledger>()
            .Property(l => l.credit_amount).HasPrecision(18, 2);

        modelBuilder.Entity<Ledger>()
            .Property(l => l.running_balance).HasPrecision(18, 2);

        // Relationships (important for delete behavior)

        modelBuilder.Entity<SubCategory>()
            .HasOne(sc => sc.Category)
            .WithMany()
            .HasForeignKey(sc => sc.categoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Item>()
            .HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.categoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Item>()
            .HasOne(i => i.SubCategory)
            .WithMany()
            .HasForeignKey(i => i.subCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Item>()
            .HasOne(i => i.Brand)
            .WithMany()
            .HasForeignKey(i => i.brandId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Item>()
            .HasOne(i => i.UOM)
            .WithMany()
            .HasForeignKey(i => i.uomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Permission>()
            .HasOne(p => p.Role)
            .WithMany()
            .HasForeignKey(p => p.roleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Permission>()
            .HasOne(p => p.Module)
            .WithMany()
            .HasForeignKey(p => p.moduleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Permission>()
            .HasOne(p => p.Component)
            .WithMany()
            .HasForeignKey(p => p.componentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Component>()
            .HasOne(c => c.Module)
            .WithMany()
            .HasForeignKey(c => c.moduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.companyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.roleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PaymentVoucher>()
            .HasOne(p => p.Company)
            .WithMany()
            .HasForeignKey(p => p.companyId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PaymentVoucher>()
            .HasOne(p => p.Vender)
            .WithMany()
            .HasForeignKey(p => p.venderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<PaymentVoucher>()
            .HasOne(p => p.Bank)
            .WithMany()
            .HasForeignKey(p => p.bankAccountId)
            .OnDelete(DeleteBehavior.NoAction);

        base.OnModelCreating(modelBuilder);
    }
}
