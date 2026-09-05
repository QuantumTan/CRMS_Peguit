using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.infrastructure.data;

namespace CRMS_Peguit.winforms.Models.Services
{
    public static class LocalDb
    {
        public static string ConnectionString =>
            "Server=localhost\\SQLEXPRESS;Database=CRMS_Local;Trusted_Connection=True;TrustServerCertificate=True;";

        public static RealEstateDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RealEstateDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            return new RealEstateDbContext(options, tenantId: 1);
        }
    }
}