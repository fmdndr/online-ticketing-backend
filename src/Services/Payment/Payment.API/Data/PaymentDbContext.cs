using Microsoft.EntityFrameworkCore;
using Payment.API.Entities;

namespace Payment.API.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentRecord> Payments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
        });
    }
}
