using Inventory_Management.Application.Features.Queries.Suppliers_DeliveryQuery;
using Inventory_Management.Application.Features.Results.Suppliers_DeliveryResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Suppliers_DeliveryHandler
{
    public class GetSuppliers_DeliveryByIdQueryHandler : IRequestHandler<GetSuppliers_DeliveryByIdQuery, GetSuppliers_DeliveryByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetSuppliers_DeliveryByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetSuppliers_DeliveryByIdQueryResult> Handle(GetSuppliers_DeliveryByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Suppliers_Deliveries.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetSuppliers_DeliveryByIdQueryResult
            {
                Id = val.Id,
                SupplierId = val.SupplierId,
                RuleId = val.RuleId,
                Description = val.Description,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}