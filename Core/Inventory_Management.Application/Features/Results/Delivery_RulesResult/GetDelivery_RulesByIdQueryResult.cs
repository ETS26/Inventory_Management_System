
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Delivery_RulesResult
{
    public class GetDelivery_RulesByIdQueryResult : BaseEntity
    {
        public string RuleName { get; set; }
        public string RuleDescription { get; set; }
    }
}
