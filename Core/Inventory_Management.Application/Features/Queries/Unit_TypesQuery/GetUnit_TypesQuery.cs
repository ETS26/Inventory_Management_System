
using Inventory_Management.Application.Features.Results.Unit_TypesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.Unit_TypesQuery
{
    public class GetUnit_TypesQuery : IRequest<List<GetUnit_TypesQueryResult>>
    {
    }
}
