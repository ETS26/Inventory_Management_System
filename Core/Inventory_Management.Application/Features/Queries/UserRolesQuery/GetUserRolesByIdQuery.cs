
using Inventory_Management.Application.Features.Results.UserRolesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.UserRolesQuery
{
    public class GetUserRolesByIdQuery : IRequest<GetUserRolesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetUserRolesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
