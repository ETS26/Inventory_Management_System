using Inventory_Management.Application.Features.Queries.Delivery_RulesQuery;
using Inventory_Management.Application.Features.Results.Delivery_RulesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Delivery_RulesHandler
{
    public class GetDelivery_RulesQueryHandler : IRequestHandler<GetDelivery_RulesQuery, List<GetDelivery_RulesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetDelivery_RulesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetDelivery_RulesQueryResult>> Handle(GetDelivery_RulesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Delivery_Rules.ToListAsync(cancellationToken);
            return val.Select(x => new GetDelivery_RulesQueryResult
            {
                Id = x.Id,
                SupplierId = x.SupplierId,
                RuleName = x.RuleName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Frequency = x.Frequency,
                Interval = x.Interval,
                ArrivalTime = x.ArrivalTime,
                DaysOfWeek = x.DaysOfWeek,
                DaysOfMonth = x.DaysOfMonth,
                LeadTimeDays = x.LeadTimeDays,
                CalendarColor = x.CalendarColor,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive
            }).ToList();
        }
    }
}