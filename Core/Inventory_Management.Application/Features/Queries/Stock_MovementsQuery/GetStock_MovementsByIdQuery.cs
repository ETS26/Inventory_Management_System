
using Inventory_Management.Application.Features.Results.Stock_MovementsResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.Stock_MovementsQuery
{
    public class GetStock_MovementsByIdQuery : IRequest<GetStock_MovementsByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetStock_MovementsByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
