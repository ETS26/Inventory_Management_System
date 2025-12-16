using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.ProductsCommand
{
    public class ActivateProductCommand : IRequest
    {
        public Guid Id { get; set; }

        public ActivateProductCommand(Guid id)
        {
            Id = id;
        }
    }
}
