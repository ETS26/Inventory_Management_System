using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.RolesCommand
{
    public class UpdateRolesCommand : IRequest
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
    }
}