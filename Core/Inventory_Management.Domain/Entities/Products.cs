using Inventory_Management.Domain;

namespace Inventory_Management.Domain.Entities
{
    public class Products : BaseEntity
    {
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public Guid CategoryId { get; set; }
        public virtual Categories Category { get; set; }
        public Guid UnitTypeId { get; set; }
        public virtual Unit_Types UnitType { get; set; }
        public string ImageURL { get; set; }
        public string Barcode { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        
        
    }
}
