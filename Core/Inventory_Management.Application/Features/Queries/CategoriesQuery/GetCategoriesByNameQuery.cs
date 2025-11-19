using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventory_Management.Application.Features.Results.CategoriesResult;

namespace Inventory_Management.Application.Features.Queries.CategoriesQuery
{
    public class GetCategoriesByNameQuery:IRequest<GetCategoriesByNameQueryResult>
    {
        public string CategoryName { get; set; }
        public GetCategoriesByNameQuery(string name)
        {
            CategoryName = name;
        }
    }
}
