using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Delivery_RulesCommand
{
    public class CreateDelivery_RulesCommand : IRequest
    {
        public string RuleName { get; set; }
        public string RuleDescription { get; set; }
    }
}