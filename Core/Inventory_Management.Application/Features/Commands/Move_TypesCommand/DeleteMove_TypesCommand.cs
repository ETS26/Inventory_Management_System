using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Move_TypesCommand
{
    public class DeleteMove_TypesCommand : IRequest
    {
        public DeleteMove_TypesCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}