
using Inventory_Management.Application.Features.Results.RolesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.RolesQuery
{
    public class GetRolesQuery : IRequest<List<GetRolesQueryResult>>
    {
    }
}
