using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.InventoriesCommand
{
    public class DeleteInventoriesCommand : IRequest
    {
        public DeleteInventoriesCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}