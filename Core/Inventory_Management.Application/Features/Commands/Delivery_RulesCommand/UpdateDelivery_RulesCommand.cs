using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Delivery_RulesCommand
{
    public class UpdateDelivery_RulesCommand : IRequest
    {
        public Guid Id { get; set; }
        public string RuleName { get; set; }
        public string RuleDescription { get; set; }
        public bool IsActive { get; set; }
    }
}