using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class CustomerController : IDisposable
    {
        private readonly RealEstateDbContext _db;

        // TODO: replace with the logged-in user's tenant id (from session).
        private readonly int _tenantId = 1;

        public CustomerController()
        {
            var connectionString =
                Environment.GetEnvironmentVariable("CRMS_CONNECTION")
                ?? throw new InvalidOperationException(
                    "CRMS_CONNECTION environment variable is not set.");

            var options = new DbContextOptionsBuilder<RealEstateDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            _db = new RealEstateDbContext(options, _tenantId);
        }

        public List<Customer> GetAll()
        {
            return _db.Customers
                .AsNoTracking()
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToList();
        }

        public Customer? GetById(int id)
        {
            return _db.Customers
                .AsNoTracking()
                .SingleOrDefault(x => x.CustomerId == id);
        }

        public Customer Add(Customer customer)
        {
            customer.TenantId = _tenantId;
            customer.CreatedAt = DateTime.UtcNow;
            customer.IsDeleted = false;
            customer.DeletedAt = null;

            _db.Customers.Add(customer);
            _db.SaveChanges();
            return customer;
        }

        public void Update(Customer customer)
        {
            var item = _db.Customers
                .SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            item.FirstName = customer.FirstName;
            item.MiddleName = customer.MiddleName;
            item.LastName = customer.LastName;
            item.Suffix = customer.Suffix;
            item.Phone = customer.Phone;
            item.Email = customer.Email;
            item.Type = customer.Type;
            item.Status = customer.Status;
            item.AssignedAgentId = customer.AssignedAgentId;

            _db.SaveChanges();
        }

        public void SoftDelete(Customer customer)
        {
            var item = _db.Customers
                .SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            _db.SaveChanges();
        }

        // Restore needs IgnoreQueryFilters, because the global filter
        // hides IsDeleted == true rows.
        public void Restore(Customer customer)
        {
            var item = _db.Customers
                .IgnoreQueryFilters()
                .SingleOrDefault(x => x.CustomerId == customer.CustomerId);
            if (item is null) return;

            item.IsDeleted = false;
            item.DeletedAt = null;
            _db.SaveChanges();
        }

        // Kept as an alias in case older code still calls Delete().
        public void Delete(Customer customer) => SoftDelete(customer);

        public void Dispose() => _db.Dispose();
    }
}