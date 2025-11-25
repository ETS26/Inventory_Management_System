using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.InventoriesCommand
{
    public class CreateInventoriesCommand : IRequest
    {
        public Guid CompanyId { get; set; }
        public Guid ProductId { get; set; }
        public string? BatchNumber { get; set; }
        public int Quantity { get; set; }
        public int CriticalStockQuantity { get; set; }
        public float PurchasePrice { get; set; }
        public float SalePrice { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Description { get; set; }
    }
}