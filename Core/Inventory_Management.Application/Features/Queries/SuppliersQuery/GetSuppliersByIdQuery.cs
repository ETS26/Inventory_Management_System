
using Inventory_Management.Application.Features.Results.SuppliersResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.SuppliersQuery
{
    public class GetSuppliersByIdQuery : IRequest<GetSuppliersByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetSuppliersByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
