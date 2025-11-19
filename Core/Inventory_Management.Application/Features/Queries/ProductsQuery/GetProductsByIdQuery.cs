
using Inventory_Management.Application.Features.Results.ProductsResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.ProductsQuery
{
    public class GetProductsByIdQuery : IRequest<GetProductsByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetProductsByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
