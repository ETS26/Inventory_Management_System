
using Inventory_Management.Application.Features.Results.UserRolesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.UserRolesQuery
{
    public class GetUserRolesQuery : IRequest<List<GetUserRolesQueryResult>>
    {
    }
}
