
using System;
using Inventory_Management.Domain.Entities;
namespace Inventory_Management.Application.Features.Results.UsersResult
{
    public class GetUsersQueryResult : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
    }
}
