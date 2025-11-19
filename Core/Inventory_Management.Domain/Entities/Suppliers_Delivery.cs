namespace Inventory_Management.Domain.Entities
{
    public class Suppliers_Delivery : BaseEntity
    {
        public Guid SupplierId { get; set; }
        public virtual Suppliers Supplier { get; set; }
        public Guid RuleId { get; set; }
        public virtual Delivery_Rules Rule { get; set; }
        public string Description { get; set; }
    }
}