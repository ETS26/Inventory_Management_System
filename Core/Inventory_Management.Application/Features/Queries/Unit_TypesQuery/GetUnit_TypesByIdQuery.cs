
using Inventory_Management.Application.Features.Results.Unit_TypesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.Unit_TypesQuery
{
    public class GetUnit_TypesByIdQuery : IRequest<GetUnit_TypesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetUnit_TypesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
