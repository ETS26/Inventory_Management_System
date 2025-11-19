
using Inventory_Management.Application.Features.Results.InventoriesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.InventoriesQuery
{
    public class GetInventoriesQuery : IRequest<List<GetInventoriesQueryResult>>
    {
    }
}
