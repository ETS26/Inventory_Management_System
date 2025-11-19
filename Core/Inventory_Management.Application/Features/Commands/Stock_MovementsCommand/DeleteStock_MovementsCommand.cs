using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Stock_MovementsCommand
{
    public class DeleteStock_MovementsCommand : IRequest
    {
        public DeleteStock_MovementsCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}