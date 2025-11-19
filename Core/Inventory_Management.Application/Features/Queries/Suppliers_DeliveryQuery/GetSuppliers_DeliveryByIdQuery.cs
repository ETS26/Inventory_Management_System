
using Inventory_Management.Application.Features.Results.Suppliers_DeliveryResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.Suppliers_DeliveryQuery
{
    public class GetSuppliers_DeliveryByIdQuery : IRequest<GetSuppliers_DeliveryByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetSuppliers_DeliveryByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
