using Inventory_Management.Domain;

namespace Inventory_Management.Domain.Entities
{
    public class Suppliers_Delivery : BaseEntity
    {
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public Guid SupplierId { get; set; }
        public virtual Suppliers Supplier { get; set; }
        public Guid RuleId { get; set; }
        public virtual Delivery_Rules Rule { get; set; }
        public string? Description { get; set; }
    }
}