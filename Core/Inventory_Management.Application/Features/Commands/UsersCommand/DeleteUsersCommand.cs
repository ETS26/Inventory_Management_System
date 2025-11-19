using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.UsersCommand
{
    public class DeleteUsersCommand : IRequest
    {
        public DeleteUsersCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}