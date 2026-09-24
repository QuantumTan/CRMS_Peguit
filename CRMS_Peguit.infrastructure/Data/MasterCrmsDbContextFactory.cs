using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRMS_Peguit.infrastructure.data
{
    public class MasterCrmsDbContextFactory
        : IDesignTimeDbContextFactory<MasterCrmsDbContext>
    {
        public MasterCrmsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MasterCrmsDbContext>();

            var connectionString =
                Environment.GetEnvironmentVariable("CRMS_CONNECTION")
                ?? "Server=(localdb)\\mssqllocaldb;Database=CRMS_Master;Trusted_Connection=True;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null));

            return new MasterCrmsDbContext(optionsBuilder.Options);
        }
    }
}
