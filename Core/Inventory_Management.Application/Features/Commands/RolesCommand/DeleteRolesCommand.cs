using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.RolesCommand
{
    public class DeleteRolesCommand : IRequest
    {
        public DeleteRolesCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}