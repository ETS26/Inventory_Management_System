namespace Inventory_Management.Domain.Entities
{
    public class Unit_Types : BaseEntity
    {
        public string UnitName { get; set; }

        public virtual ICollection<Products> Products { get; set; }
    }
}