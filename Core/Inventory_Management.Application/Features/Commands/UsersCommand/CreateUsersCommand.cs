using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.UsersCommand
{
    public class CreateUsersCommand : IRequest
    {
        public Guid CompanyId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
    }
}