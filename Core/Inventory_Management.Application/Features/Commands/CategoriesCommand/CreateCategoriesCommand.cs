using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.CategoriesCommand
{
    public class CreateCategoriesCommand : IRequest
    {
        public Guid CompanyId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}