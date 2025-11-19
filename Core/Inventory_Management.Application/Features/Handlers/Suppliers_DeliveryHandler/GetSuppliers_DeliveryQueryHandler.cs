using Inventory_Management.Application.Features.Queries.Suppliers_DeliveryQuery;
using Inventory_Management.Application.Features.Results.Suppliers_DeliveryResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Suppliers_DeliveryHandler
{
    public class GetSuppliers_DeliveryQueryHandler : IRequestHandler<GetSuppliers_DeliveryQuery, List<GetSuppliers_DeliveryQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetSuppliers_DeliveryQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetSuppliers_DeliveryQueryResult>> Handle(GetSuppliers_DeliveryQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Suppliers_Deliveries.ToListAsync();
            return val.Select(x => new GetSuppliers_DeliveryQueryResult
            {
                Id = x.Id,
                SupplierId = x.SupplierId,
                RuleId = x.RuleId,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}