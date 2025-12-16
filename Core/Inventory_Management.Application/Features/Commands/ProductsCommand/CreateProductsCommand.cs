using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.ProductsCommand
{
    public class CreateProductsCommand : IRequest
    {
        public Guid CategoryId { get; set; }
        public Guid UnitTypeId { get; set; }
        public string Barcode { get; set; }
        public string ProductName { get; set; }
        public string ImageURL { get; set; }
        public string Description { get; set; }
    }
}