namespace Inventory_Management.Domain.Entities
{
    public class Users : BaseEntity
    {
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }

        public virtual ICollection<UsersRoles> UsersRoles { get; set; }
        public virtual ICollection<Stock_Movements> Stock_Movements { get; set; }
    }
}