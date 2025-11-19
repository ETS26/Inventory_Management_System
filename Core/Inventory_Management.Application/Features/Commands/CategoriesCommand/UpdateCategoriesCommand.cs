using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.CategoriesCommand
{
    public class UpdateCategoriesCommand : IRequest
    {
        public Guid Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}