
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Suppliers_DeliveryResult
{
    public class GetSuppliers_DeliveryByIdQueryResult : BaseEntity
    {
        public Guid SupplierId { get; set; }
        public Guid RuleId { get; set; }
        public string Description { get; set; }
    }
}
