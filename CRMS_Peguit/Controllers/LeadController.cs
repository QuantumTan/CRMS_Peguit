using System;
using System.Collections.Generic;
using System.Linq;
using CRMS_Peguit.domain.entities;
using CRMS_Peguit.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRMS_Peguit.winforms.Controllers
{
    public class LeadController : IDisposable
    {
        private readonly RealEstateDbContext _db;

        // TODO: replace with the logged-in user's tenant id (from session).
        private readonly int _tenantId = 1;

        public LeadController()
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

        public List<Lead> GetAll()
        {
            return _db.Leads
                .AsNoTracking()
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToList();
        }

        public Lead? GetById(int id)
        {
            return _db.Leads
                .AsNoTracking()
                .SingleOrDefault(x => x.LeadId == id);
        }

        public Lead Add(Lead lead)
        {
            lead.TenantId = _tenantId;
            lead.CreatedAt = DateTime.UtcNow;
            lead.IsDeleted = false;
            lead.DeletedAt = null;

            _db.Leads.Add(lead);
            _db.SaveChanges();
            return lead;
        }

        public void Update(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.FirstName = lead.FirstName;
            item.MiddleName = lead.MiddleName;
            item.LastName = lead.LastName;
            item.Suffix = lead.Suffix;
            item.Phone = lead.Phone;
            item.Email = lead.Email;
            item.Source = lead.Source;
            item.Stage = lead.Stage;
            item.AssignedAgentId = lead.AssignedAgentId;

            _db.SaveChanges();
        }

        public void SoftDelete(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            _db.SaveChanges();
        }

        // Restore needs IgnoreQueryFilters, because the global filter
        // hides IsDeleted == true rows.
        public void Restore(Lead lead)
        {
            var item = _db.Leads
                .IgnoreQueryFilters()
                .SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null) return;

            item.IsDeleted = false;
            item.DeletedAt = null;
            _db.SaveChanges();
        }

        public Customer ConvertToCustomer(Lead lead)
        {
            var item = _db.Leads.SingleOrDefault(x => x.LeadId == lead.LeadId);
            if (item is null)
                throw new InvalidOperationException("Lead not found.");

            if (string.Equals(item.Stage, "converted",
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "This lead has already been converted.");

            var customer = new Customer
            {
                TenantId = item.TenantId,
                FirstName = item.FirstName,
                MiddleName = item.MiddleName,
                LastName = item.LastName,
                Suffix = item.Suffix,
                Email = item.Email,
                Phone = item.Phone,
                Type = "buyer",
                Status = "active",
                AssignedAgentId = item.AssignedAgentId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                DeletedAt = null
            };

            _db.Customers.Add(customer);
            _db.SaveChanges();

            item.Stage = "converted";
            item.ConvertedCustomerId = customer.CustomerId;
            _db.SaveChanges();

            return customer;
        }

        public void Dispose() => _db.Dispose();
    }
}