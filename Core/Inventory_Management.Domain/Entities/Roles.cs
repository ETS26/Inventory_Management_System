namespace Inventory_Management.Domain.Entities
{
    public class Roles : BaseEntity
    {
        public string RoleName { get; set; }

        public virtual ICollection<UsersRoles> UsersRoles { get; set; }
    }
}