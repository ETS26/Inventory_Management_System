
using System;
using Inventory_Management.Domain.Entities;
namespace Inventory_Management.Application.Features.Results.CategoriesResult
{
    public class GetCategoriesQueryResult:BaseEntity
    {
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
