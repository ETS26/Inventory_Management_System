
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.SuppliersResult
{
    public class GetSuppliersQueryResult : BaseEntity
    {
        public Guid CompanyId { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
    }
}
