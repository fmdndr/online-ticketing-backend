using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payment.API.Data;

public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ticketing_payments;Username=postgres;Password=postgres");

        return new PaymentDbContext(optionsBuilder.Options);
    }
}
