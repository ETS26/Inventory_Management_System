
using Inventory_Management.Application.Features.Results.UsersResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.UsersQuery
{
    public class GetUsersQuery : IRequest<List<GetUsersQueryResult>>
    {
    }
}
