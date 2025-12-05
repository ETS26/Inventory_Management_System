using Inventory_Management.Domain;
using Inventory_Management.Domain.Common;
using System.Text.Json.Serialization;

namespace Inventory_Management.Domain.Entities
{
    public class Stock_Movements : BaseEntity,IHasCompany
    {
        public Guid InventoryId { get; set; }
        public virtual Inventories Inventory { get; set; }
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public Guid MoveTypeId { get; set; }
        public virtual Move_Types MoveType { get; set; }
        public Guid SupplierId { get; set; }
        public virtual Suppliers Supplier { get; set; }
        public Guid UserId { get; set; }
        public virtual Users User { get; set; }
        public int Quantity { get; set; }
        public float Payment { get; set; }
        public string? Description { get; set; }

    }
}