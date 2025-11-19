namespace Inventory_Management.Domain.Entities
{
    public class Companies : BaseEntity
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        public virtual ICollection<Inventories> Inventories { get; set; }
        public virtual ICollection<Users> Users { get; set; }
        public virtual ICollection<Stock_Movements> Stock_Movements { get; set; }
    }
}