using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Features.Results.CategoriesResult
{
    public class GetCategoriesByNameQueryResult:BaseEntity
    {
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
}
