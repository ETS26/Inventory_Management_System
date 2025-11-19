using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.RolesCommand
{
    public class CreateRolesCommand : IRequest
    {
        public string RoleName { get; set; }
    }
}