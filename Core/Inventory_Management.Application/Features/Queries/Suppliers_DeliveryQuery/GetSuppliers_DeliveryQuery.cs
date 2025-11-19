
using Inventory_Management.Application.Features.Results.Suppliers_DeliveryResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.Suppliers_DeliveryQuery
{
    public class GetSuppliers_DeliveryQuery : IRequest<List<GetSuppliers_DeliveryQueryResult>>
    {
    }
}
