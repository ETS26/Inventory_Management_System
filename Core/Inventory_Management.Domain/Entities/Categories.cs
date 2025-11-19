namespace Inventory_Management.Domain.Entities
{
    public class Categories : BaseEntity
    {
        public string CategoryName { get; set; }
        public string Description { get; set; }

        public virtual ICollection<Products> Products { get; set; }
    }
}