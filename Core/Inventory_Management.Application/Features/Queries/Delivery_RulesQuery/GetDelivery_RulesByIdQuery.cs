
using Inventory_Management.Application.Features.Results.Delivery_RulesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.Delivery_RulesQuery
{
    public class GetDelivery_RulesByIdQuery : IRequest<GetDelivery_RulesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetDelivery_RulesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
