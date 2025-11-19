
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.CompaniesResult
{
    public class GetCompaniesByIdQueryResult : BaseEntity
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }
}
