using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Suppliers_DeliveryCommand
{
    public class UpdateSuppliers_DeliveryCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public Guid RuleId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}