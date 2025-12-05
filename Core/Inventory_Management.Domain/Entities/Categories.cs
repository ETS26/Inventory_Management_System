using Inventory_Management.Domain;
using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities
{
    public class Categories : BaseEntity,IHasCompany
    {
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public string CategoryName { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<Products> Products { get; set; }
    }
}