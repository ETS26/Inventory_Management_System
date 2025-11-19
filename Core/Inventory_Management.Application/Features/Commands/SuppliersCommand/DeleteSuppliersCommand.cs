using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.SuppliersCommand
{
    public class DeleteSuppliersCommand : IRequest
    {
        public DeleteSuppliersCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}