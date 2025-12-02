using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.SuppliersCommand
{
    public class UpdateSuppliersCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
    }
}