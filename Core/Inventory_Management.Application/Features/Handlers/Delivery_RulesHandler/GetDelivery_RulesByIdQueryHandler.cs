using Inventory_Management.Application.Features.Queries.Delivery_RulesQuery;
using Inventory_Management.Application.Features.Results.Delivery_RulesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Delivery_RulesHandler
{
    public class GetDelivery_RulesByIdQueryHandler : IRequestHandler<GetDelivery_RulesByIdQuery, GetDelivery_RulesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetDelivery_RulesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetDelivery_RulesByIdQueryResult> Handle(GetDelivery_RulesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Delivery_Rules.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetDelivery_RulesByIdQueryResult
            {
                Id = val.Id,
                RuleName = val.RuleName,
                RuleDescription = val.RuleDescription,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}