using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.ProductsCommand
{
    public class DeleteProductsCommand : IRequest
    {
        public DeleteProductsCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}