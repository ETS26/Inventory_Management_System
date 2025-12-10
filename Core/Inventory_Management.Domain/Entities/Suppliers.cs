using Inventory_Management.Domain;
using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities
{
    public class Suppliers : BaseEntity, IHasCompany
    {
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public string SupplierName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        public virtual ICollection<Delivery_Rules> Delivery_Rules { get; set; }
        public virtual ICollection<Stock_Movements> Stock_Movements { get; set; }
    }
}