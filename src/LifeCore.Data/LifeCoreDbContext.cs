using LifeCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeCore.Data;

public sealed class LifeCoreDbContext(DbContextOptions<LifeCoreDbContext> options) : DbContext(options)
{
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<UnderwritingCase> Cases => Set<UnderwritingCase>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<Coverage> Coverages => Set<Coverage>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<FundAllocation> FundAllocations => Set<FundAllocation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PolicyTransaction> PolicyTransactions => Set<PolicyTransaction>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot ORDER BY/compare DateTimeOffset natively; store as sortable binary ticks.
        if (Database.IsSqlite())
        {
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();
        }
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Party>().OwnsOne(p => p.Address);
        b.Entity<Product>().HasKey(p => p.Code);
        b.Entity<UnderwritingCase>().HasIndex(c => c.CaseNumber).IsUnique();
        b.Entity<UnderwritingCase>().HasIndex(c => new { c.Status, c.AssignedUnderwriter });
        b.Entity<UnderwritingCase>().HasIndex(c => c.Priority);
        b.Entity<UnderwritingCase>().HasOne(c => c.Product).WithMany().HasForeignKey(c => c.ProductCode);
        b.Entity<UnderwritingCase>().HasMany(c => c.Requirements).WithOne(r => r.Case).HasForeignKey(r => r.CaseNumber).HasPrincipalKey(c => c.CaseNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UnderwritingCase>().HasMany(c => c.Tasks).WithOne(t => t.Case).HasForeignKey(t => t.CaseNumber).HasPrincipalKey(c => c.CaseNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UnderwritingCase>().HasMany(c => c.Notes).WithOne(n => n.Case).HasForeignKey(n => n.CaseNumber).HasPrincipalKey(c => c.CaseNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UnderwritingCase>().HasMany(c => c.ChatMessages).WithOne(m => m.Case).HasForeignKey(m => m.CaseNumber).HasPrincipalKey(c => c.CaseNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Requirement>().HasIndex(r => new { r.CaseNumber, r.Status, r.Type });
        b.Entity<WorkTask>().HasIndex(t => new { t.Status, t.AssignedTo });
        b.Entity<Policy>().HasIndex(p => p.PolicyNumber).IsUnique();
        b.Entity<Policy>().HasIndex(p => p.Status);
        b.Entity<Policy>().HasIndex(p => p.OwnerId);
        b.Entity<Policy>().HasIndex(p => p.InsuredId);
        b.Entity<Policy>().HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductCode);
        b.Entity<Policy>().HasMany(p => p.Coverages).WithOne(c => c.Policy).HasForeignKey(c => c.PolicyNumber).HasPrincipalKey(p => p.PolicyNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Policy>().HasMany(p => p.Beneficiaries).WithOne(c => c.Policy).HasForeignKey(c => c.PolicyNumber).HasPrincipalKey(p => p.PolicyNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Policy>().HasMany(p => p.FundAllocations).WithOne(c => c.Policy).HasForeignKey(c => c.PolicyNumber).HasPrincipalKey(p => p.PolicyNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Policy>().HasMany(p => p.Payments).WithOne(c => c.Policy).HasForeignKey(c => c.PolicyNumber).HasPrincipalKey(p => p.PolicyNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Policy>().HasMany(p => p.Transactions).WithOne(c => c.Policy).HasForeignKey(c => c.PolicyNumber).HasPrincipalKey(p => p.PolicyNumber).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Party>().HasIndex("Name");
        b.Entity<Party>().HasIndex("SsnLast4");
    }
}
