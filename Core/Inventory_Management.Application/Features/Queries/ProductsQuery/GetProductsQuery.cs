
using Inventory_Management.Application.Features.Results.ProductsResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.ProductsQuery
{
    public class GetProductsQuery : IRequest<List<GetProductsQueryResult>>
    {
    }
}
