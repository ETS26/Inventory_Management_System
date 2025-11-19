
using Inventory_Management.Application.Features.Results.Stock_MovementsResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.Stock_MovementsQuery
{
    public class GetStock_MovementsQuery : IRequest<List<GetStock_MovementsQueryResult>>
    {
    }
}
