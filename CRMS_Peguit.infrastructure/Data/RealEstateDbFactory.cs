using System;
using System.Collections.Generic;
using System.Text;

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

            optionsBuilder.UseSqlServer(
                "Server=db66713.public.databaseasp.net; Database=db66713; User Id=db66713; Password=2Ni%Sz_9?J8m; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;"
            );

            return new RealEstateDbContext(optionsBuilder.Options);
        }
    }
}