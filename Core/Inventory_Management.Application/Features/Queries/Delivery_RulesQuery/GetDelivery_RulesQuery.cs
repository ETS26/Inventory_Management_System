
using Inventory_Management.Application.Features.Results.Delivery_RulesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.Delivery_RulesQuery
{
    public class GetDelivery_RulesQuery : IRequest<List<GetDelivery_RulesQueryResult>>
    {
    }
}
