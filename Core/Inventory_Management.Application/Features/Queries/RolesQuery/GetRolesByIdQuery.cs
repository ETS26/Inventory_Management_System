
using Inventory_Management.Application.Features.Results.RolesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.RolesQuery
{
    public class GetRolesByIdQuery : IRequest<GetRolesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetRolesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
