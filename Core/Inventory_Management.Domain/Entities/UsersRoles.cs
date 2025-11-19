namespace Inventory_Management.Domain.Entities
{
    public class UsersRoles : BaseEntity
    {
        public Guid UserId { get; set; }
        public virtual Users User { get; set; }
        public Guid RoleId { get; set; }
        public virtual Roles Role { get; set; }
    }
}