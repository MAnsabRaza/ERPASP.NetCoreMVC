using ERP.Models;
using ERP.Models.Account;
using Microsoft.Build.ObjectModelRemoting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Company { get; set; }
    public DbSet<Role> Role { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<Module> Module { get; set; }
    public DbSet<Component> Component { get; set; }
    public DbSet<Permission> Permission { get; set; }
    public DbSet<Category> Category { get; set; }
    public DbSet<SubCategory> SubCategory { get; set; }
    public DbSet<Brand> Brand { get; set; }
    public DbSet<UOM> UOM { get; set; }
    public DbSet<Item> Item { get; set; }
    public DbSet<Customer> Customer { get; set; }
    public DbSet<Vender> Vender { get; set; }
    
    public DbSet<Transporter> Transporter { get; set; }
    public DbSet<Warehouse> Warehouse { get; set; }
        
    public DbSet<PaymentVoucher> PaymentVoucher { get; set; }
    public DbSet<Bank> Bank { get; set; }
    public DbSet<AccountType> AccountType { get; set; }
    public DbSet<ChartOfAccount> ChartOfAccount { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentVoucher>()
    .Property(p => p.amount)
    .HasPrecision(18, 2);
        modelBuilder.Entity<Bank>()
   .Property(p => p.opening_balance)
   .HasPrecision(18, 2);
        modelBuilder.Entity<Customer>()
            .Property(c => c.credit_limit)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Customer>()
            .Property(c => c.current_balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Vender>()
            .Property(v => v.current_balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.purchase_rate)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.sale_rate)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.rate)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.discount_amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.total_amount)
            .HasPrecision(18, 2);
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

        base.OnModelCreating(modelBuilder);
    }
}