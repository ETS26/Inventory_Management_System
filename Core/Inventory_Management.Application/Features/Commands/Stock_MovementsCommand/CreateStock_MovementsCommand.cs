using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Stock_MovementsCommand
{
    public class CreateStock_MovementsCommand : IRequest
    {
        public Guid CompanyId { get; set; }
        public Guid? InventoryId { get; set; }
        public Guid MoveTypeId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }

        public bool IsNewInventory { get; set; } // Kutucuk seçili mi?
        public Guid? ProductId { get; set; }     // Hangi ürün için kart açýlacak?
        public float PurchasePrice { get; set; }
        public float SalePrice { get; set; }
        public int CriticalStockQuantity { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}