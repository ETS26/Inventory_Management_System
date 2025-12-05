using Inventory_Management.Domain;
using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities
{
    public class Inventories : BaseEntity, IHasCompany
    {
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public Guid ProductId { get; set; }
        public virtual Products Product { get; set; }
        public string BatchNumber { get; set; }
        public int Quantity { get; set; }
        public int CriticalStockQuantity { get; set; }
        public float PurchasePrice { get; set; }
        public float SalePrice { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Description { get; set; }
    }
}