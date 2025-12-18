using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.SuppliersCommand
{
    public class ActivateSupplierCommand : IRequest
    {
        public Guid Id { get; }

        public ActivateSupplierCommand(Guid id)
        {
            Id = id;
        }
    }
}
