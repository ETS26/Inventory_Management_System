
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.InventoriesResult
{
    public class GetInventoriesQueryResult : BaseEntity
    {
        public string? BatchNumber { get; set; }
        public int Quantity { get; set; }
        public int CriticalStockQuantity { get; set; }
        public float PurchasePrice { get; set; }
        public float SalePrice { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Description { get; set; }

        // --- Ýliþkili Tablolardan Alýnan Düz Veriler (DTO Mantýðý) ---
        // Nesne (Entity) yerine sadece isimlerini taþýyoruz.

        public string ProductName { get; set; }      // Product.ProductName
        public string Barcode { get; set; }          // Product.Barcode
        public string CategoryName { get; set; }     // Product.Category.CategoryName
        public string UnitTypeName { get; set; }     // Product.UnitType.UnitName
        public string CompanyName { get; set; }

        public bool IsActive { get; set; }
    }
}
