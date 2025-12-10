using Inventory_Management.Domain.Entities;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Delivery_RulesCommand
{
    public class CreateDelivery_RulesCommand : IRequest
    {
        public Guid CompanyId { get; set; }
        public Guid SupplierId { get; set; }
        public string RuleName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Delivery_Rules.FrequencyType Frequency { get; set; }
        public int Interval { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public string? DaysOfWeek { get; set; }
        public int? DayOfMonth { get; set; }
        public int LeadTimeDays { get; set; }
        public string CalendarColor { get; set; }
    }
}