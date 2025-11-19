
using Inventory_Management.Application.Features.Results.Move_TypesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.Move_TypesQuery
{
    public class GetMove_TypesQuery : IRequest<List<GetMove_TypesQueryResult>>
    {
    }
}
