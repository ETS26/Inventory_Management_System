using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.UsersRolesCommand
{
    public class CreateUsersRolesCommand : IRequest
    {
        public Guid CompanyId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}