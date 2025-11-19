using Inventory_Management.Application.Features.Queries.InventoriesQuery;
using Inventory_Management.Application.Features.Results.InventoriesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.InventoriesHandler
{
    public class GetInventoriesQueryHandler : IRequestHandler<GetInventoriesQuery, List<GetInventoriesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetInventoriesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetInventoriesQueryResult>> Handle(GetInventoriesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Inventories.ToListAsync();
            return val.Select(x => new GetInventoriesQueryResult
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                BatchNumber = x.BatchNumber,
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                CriticalStockQuantity = x.CriticalStockQuantity,
                PurchasePrice = x.PurchasePrice,
                SalePrice = x.SalePrice,
                ExpirationDate = x.ExpirationDate,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}