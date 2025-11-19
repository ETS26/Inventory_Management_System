using Inventory_Management.Application.Features.Queries.Delivery_RulesQuery;
using Inventory_Management.Application.Features.Results.Delivery_RulesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
            var val = await _context.Delivery_Rules.ToListAsync();
            return val.Select(x => new GetDelivery_RulesQueryResult
            {
                Id = x.Id,
                RuleName = x.RuleName,
                RuleDescription = x.RuleDescription,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}