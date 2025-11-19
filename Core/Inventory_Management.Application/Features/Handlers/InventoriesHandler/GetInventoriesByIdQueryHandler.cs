using Inventory_Management.Application.Features.Queries.InventoriesQuery;
using Inventory_Management.Application.Features.Results.InventoriesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.InventoriesHandler
{
    public class GetInventoriesByIdQueryHandler : IRequestHandler<GetInventoriesByIdQuery, GetInventoriesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetInventoriesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetInventoriesByIdQueryResult> Handle(GetInventoriesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Inventories.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetInventoriesByIdQueryResult
            {
                Id = val.Id,
                BatchNumber = val.BatchNumber,
                CompanyId = val.CompanyId,
                ProductId = val.ProductId,
                Quantity = val.Quantity,
                CriticalStockQuantity = val.CriticalStockQuantity,
                PurchasePrice = val.PurchasePrice,
                SalePrice = val.SalePrice,
                ExpirationDate = val.ExpirationDate,
                Description = val.Description,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}