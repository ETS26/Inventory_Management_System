using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.CategoriesCommand
{
    public class DeleteCategoriesCommand : IRequest
    {
        public Guid Id { get; set; }

        public DeleteCategoriesCommand(Guid id)
        {
            Id = id;
        }
    }
}