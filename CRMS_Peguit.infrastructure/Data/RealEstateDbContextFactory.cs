using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRMS_Peguit.infrastructure.data
{
    public class RealEstateDbContextFactory
        : IDesignTimeDbContextFactory<RealEstateDbContext>
    {
        public RealEstateDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RealEstateDbContext>();

            var connectionString =
                Environment.GetEnvironmentVariable("CRMS_CONNECTION")
                ?? throw new InvalidOperationException(
                    "CRMS_CONNECTION environment variable is not set.");

            optionsBuilder.UseSqlServer(connectionString);

            return new RealEstateDbContext(optionsBuilder.Options);
        }
    }
}