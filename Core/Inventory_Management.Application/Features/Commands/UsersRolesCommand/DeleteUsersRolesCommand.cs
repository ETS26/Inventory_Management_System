using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.UsersRolesCommand
{
    public class DeleteUsersRolesCommand : IRequest
    {
        public DeleteUsersRolesCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}