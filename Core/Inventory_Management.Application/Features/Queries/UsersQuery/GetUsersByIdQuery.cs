
using Inventory_Management.Application.Features.Results.UsersResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.UsersQuery
{
    public class GetUsersByIdQuery : IRequest<GetUsersByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetUsersByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
