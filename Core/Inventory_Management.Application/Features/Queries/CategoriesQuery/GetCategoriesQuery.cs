
using Inventory_Management.Application.Features.Results.CategoriesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.CategoriesQuery
{
    public class GetCategoriesQuery : IRequest<List<GetCategoriesQueryResult>>
    {
    }
}
