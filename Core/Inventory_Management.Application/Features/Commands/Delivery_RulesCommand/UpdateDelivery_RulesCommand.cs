using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Delivery_RulesCommand
{
    public class UpdateDelivery_RulesCommand : IRequest
    {
        public Guid Id { get; set; }
        public string RuleName { get; set; }
        public string? RuleDescription { get; set; }
        public int LeadTimeDays { get; set; }
        public string CalendarColor { get; set; }
        public bool IsMonday { get; set; }
        public bool IsTuesday { get; set; }
        public bool IsWednesday { get; set; }
        public bool IsThursday { get; set; }
        public bool IsFriday { get; set; }
        public bool IsSaturday { get; set; }
        public bool IsSunday { get; set; }       
        public bool IsActive { get; set; }
    }
}