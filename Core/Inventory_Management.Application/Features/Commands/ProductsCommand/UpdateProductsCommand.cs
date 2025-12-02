using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.ProductsCommand
{
    public class UpdateProductsCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UnitTypeId { get; set; }
        public string Barcode { get; set; }
        public string ProductName { get; set; }
        public string ImageURL { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}