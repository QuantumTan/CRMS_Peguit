using Microsoft.EntityFrameworkCore;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CRMS_Peguit.infrastructure.repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int customerId);
        Task<IList<Customer>> GetAllAsync();
        Task<IList<Customer>> GetActiveAsync();
        Task<IList<Customer>> GetDeletedAsync();
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task SoftDeleteAsync(int customerId);
        Task RestoreAsync(int customerId);
        Task SaveChangesAsync();
    }

    public class CustomerRepository : ICustomerRepository
    {
        private readonly MasterCrmsDbContext _dbContext;

        public CustomerRepository(MasterCrmsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Customer?> GetByIdAsync(int customerId)
        {
            // Automatically excludes soft-deleted records due to global query filter
            return await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<IList<Customer>> GetAllAsync()
        {
            // Returns only non-deleted customers
            return await _dbContext.Customers.ToListAsync();
        }

        public async Task<IList<Customer>> GetActiveAsync()
        {
            return await _dbContext.Customers
                .Where(c => c.Status == "Active")
                .ToListAsync();
        }

        /// <summary>
        /// Get soft-deleted customers (requires bypassing the global query filter)
        /// </summary>
        public async Task<IList<Customer>> GetDeletedAsync()
        {
            return await _dbContext.Customers
                .IgnoreQueryFilters()
                .Where(c => c.IsDeleted)
                .ToListAsync();
        }

        public async Task AddAsync(Customer customer)
        {
            customer.IsDeleted = false;
            customer.DeletedAt = null;
            await _dbContext.Customers.AddAsync(customer);
        }

        public async Task UpdateAsync(Customer customer)
        {
            _dbContext.Customers.Update(customer);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Soft delete a customer
        /// </summary>
        public async Task SoftDeleteAsync(int customerId)
        {
            var customer = await _dbContext.Customers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer != null)
            {
                customer.IsDeleted = true;
                customer.DeletedAt = DateTime.UtcNow;
                _dbContext.Customers.Update(customer);
            }
        }

        /// <summary>
        /// Restore a soft-deleted customer
        /// </summary>
        public async Task RestoreAsync(int customerId)
        {
            var customer = await _dbContext.Customers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer != null)
            {
                customer.IsDeleted = false;
                customer.DeletedAt = null;
                _dbContext.Customers.Update(customer);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}