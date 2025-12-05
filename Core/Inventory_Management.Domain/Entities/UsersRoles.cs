using Inventory_Management.Domain.Common;

namespace Inventory_Management.Domain.Entities
{
    public class UsersRoles : BaseEntity, IHasCompany
    {
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public Guid UserId { get; set; }
        public virtual Users User { get; set; }
        public Guid RoleId { get; set; }
        public virtual Roles Role { get; set; }
    }
}