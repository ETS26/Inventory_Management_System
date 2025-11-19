using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.UsersCommand
{
    public class UpdateUsersCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }
    }
}