using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.UsersRolesCommand
{
    public class UpdateUsersRolesCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}