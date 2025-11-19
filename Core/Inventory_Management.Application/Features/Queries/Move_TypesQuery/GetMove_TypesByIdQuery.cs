
using Inventory_Management.Application.Features.Results.Move_TypesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.Move_TypesQuery
{
    public class GetMove_TypesByIdQuery : IRequest<GetMove_TypesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetMove_TypesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
