
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.ProductsResult
{
    public class GetProductsByIdQueryResult : BaseEntity
    {
        public Guid CategoryId { get; set; }
        public Guid UnitTypeId { get; set; }
        public string Barcode { get; set; }
        public string ImageURL { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
    }
}
