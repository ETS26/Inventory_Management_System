
using Inventory_Management.Application.Features.Results.InventoriesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.InventoriesQuery
{
    public class GetInventoriesByIdQuery : IRequest<GetInventoriesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetInventoriesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
