namespace Inventory_Management.Domain.Entities
{
    public class Suppliers : BaseEntity
    {
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        public virtual ICollection<Suppliers_Delivery> Suppliers_Deliveries { get; set; }
        public virtual ICollection<Stock_Movements> Stock_Movements { get; set; }
    }
}