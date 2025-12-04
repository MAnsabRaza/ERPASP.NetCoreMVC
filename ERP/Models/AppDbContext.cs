using ERP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;

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
        // ✅ ValueConverters for DateOnly / DateOnly?
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));

        var nullableDateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
            d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
            d => d.HasValue ? DateOnly.FromDateTime(d.Value) : (DateOnly?)null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateOnly))
                {
                    property.SetValueConverter(dateOnlyConverter);
                    property.SetColumnType("date"); // ✅ force SQL column to `date`
                }

                if (property.ClrType == typeof(DateOnly?))
                {
                    property.SetValueConverter(nullableDateOnlyConverter);
                    property.SetColumnType("date"); // ✅ force SQL column to `date`
                }
            }
        }

        // ✅ Decimal precision adjustments
        modelBuilder.Entity<PaymentVoucher>().Property(p => p.amount).HasPrecision(18, 2);
        modelBuilder.Entity<Bank>().Property(p => p.opening_balance).HasPrecision(18, 2);
        modelBuilder.Entity<Customer>().Property(c => c.credit_limit).HasPrecision(18, 2);
        modelBuilder.Entity<Customer>().Property(c => c.current_balance).HasPrecision(18, 2);
        modelBuilder.Entity<Vender>().Property(v => v.current_balance).HasPrecision(18, 2);
        modelBuilder.Entity<Item>().Property(i => i.purchase_rate).HasPrecision(18, 2);
        modelBuilder.Entity<Item>().Property(i => i.sale_rate).HasPrecision(18, 2);
        modelBuilder.Entity<Item>().Property(i => i.rate).HasPrecision(18, 2);
        modelBuilder.Entity<Item>().Property(i => i.discount_amount).HasPrecision(18, 2);
        modelBuilder.Entity<Item>().Property(i => i.total_amount).HasPrecision(18, 2);
        modelBuilder.Entity<StockMaster>().Property(s => s.total_amount).HasPrecision(18, 2);
        modelBuilder.Entity<StockMaster>().Property(s => s.discount_amount).HasPrecision(18, 2);
//        modelBuilder.Entity<StockMaster>().Property(s => s.tax_amount).HasPrecision(18, 2);
        modelBuilder.Entity<StockMaster>().Property(s => s.net_amount).HasPrecision(18, 2);
        modelBuilder.Entity<StockDetail>().Property(s => s.rate).HasPrecision(18, 2);
        modelBuilder.Entity<StockDetail>().Property(s => s.amount).HasPrecision(18, 2);
        modelBuilder.Entity<StockDetail>().Property(s => s.discount_percentage).HasPrecision(18, 2);
        modelBuilder.Entity<StockDetail>().Property(s => s.discount_amount).HasPrecision(18, 2);
        modelBuilder.Entity<StockDetail>().Property(s => s.net_amount).HasPrecision(18, 2);
        modelBuilder.Entity<JournalEntry>().Property(j => j.total_debit).HasPrecision(18, 2);
        modelBuilder.Entity<JournalEntry>().Property(j => j.total_credit).HasPrecision(18, 2);
        modelBuilder.Entity<JournalDetail>().Property(j => j.debit_amount).HasPrecision(18, 2);
        modelBuilder.Entity<JournalDetail>().Property(j => j.credit_amount).HasPrecision(18, 2);
        modelBuilder.Entity<Ledger>().Property(l => l.debit_amount).HasPrecision(18, 2);
        modelBuilder.Entity<Ledger>().Property(l => l.credit_amount).HasPrecision(18, 2);
        modelBuilder.Entity<Ledger>().Property(l => l.running_balance).HasPrecision(18, 2);

        // ✅ Relationships (shortened for readability)
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
        modelBuilder.Entity<PaymentVoucher>()
            .HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.customerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Ledger>()
            .HasOne(l => l.Company)
            .WithMany()
            .HasForeignKey(l => l.companyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ledger>()
            .HasOne(l => l.ChartOfAccount)
            .WithMany()
            .HasForeignKey(l => l.chartOfAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ledger>()
            .HasOne(l => l.JournalEntry)
            .WithMany()
            .HasForeignKey(l => l.journalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMaster>()
            .HasOne(sm => sm.Company)
            .WithMany()
            .HasForeignKey(sm => sm.companyId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockMaster>()
         .HasOne(sm => sm.User)
         .WithMany()
         .HasForeignKey(je => je.userId)
         .OnDelete(DeleteBehavior.NoAction);


        modelBuilder.Entity<StockMaster>()
            .HasOne(sm => sm.Vender)
            .WithMany()
            .HasForeignKey(sm => sm.venderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMaster>()
            .HasOne(sm => sm.Customer)
            .WithMany()
            .HasForeignKey(sm => sm.customerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMaster>()
            .HasOne(sm => sm.Transporter)
            .WithMany()
            .HasForeignKey(sm => sm.transporterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockDetail>()
       .HasOne(sd => sd.StockMaster)
       .WithMany(sm => sm.StockDetail)  // now correctly references the collection
       .HasForeignKey(sd => sd.StockMasterId)
       .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockDetail>()
            .HasOne(sd => sd.Item)
            .WithMany()
            .HasForeignKey(sd => sd.itemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.Company)
            .WithMany()
            .HasForeignKey(je => je.companyId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.User)
            .WithMany()
            .HasForeignKey(je => je.userId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<JournalDetail>()
            .HasOne(je => je.ChartOfAccount)
            .WithMany()
            .HasForeignKey(je => je.chartOfAccountId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<JournalDetail>()
            .HasOne(je => je.JournalEntry)
            .WithMany()
            .HasForeignKey(je => je.journalEntryId)
            .OnDelete(DeleteBehavior.NoAction);

        base.OnModelCreating(modelBuilder);
    }
}
