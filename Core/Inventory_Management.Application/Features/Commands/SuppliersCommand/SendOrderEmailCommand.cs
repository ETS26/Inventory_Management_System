using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.SuppliersCommand
{
    public class SendOrderEmailCommand : IRequest
    {
        public Guid SupplierId { get; set; }
        public Guid? InventoryId { get; set; } // Nullable yapıldı
        public Guid? ProductId { get; set; }
        public Guid UserId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; } // Eklenen alan
        public string UserFullName { get; set; }
        public string UserCompany { get; set; }
    }
}