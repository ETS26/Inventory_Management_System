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
            var val = await _context.Inventories
                .Include(x => x.Product)
                    .ThenInclude(p => p.Category)
                .Include(x => x.Product)
                    .ThenInclude(p => p.UnitType)
                .Include(x => x.Company)
                .ToListAsync(cancellationToken);

            return val.Select(x => new GetInventoriesQueryResult
            {
                Id = x.Id,

                // --- Envanter Temel Bilgileri ---
                BatchNumber = x.BatchNumber,
                Quantity = x.Quantity,
                CriticalStockQuantity = x.CriticalStockQuantity,
                PurchasePrice = x.PurchasePrice,
                SalePrice = x.SalePrice,
                ExpirationDate = x.ExpirationDate,
                Description = x.Description,

                // --- İlişkili Tablolardan Gelen İsimler ---
                // Null kontrolü (?) yaparak hata almayı engelliyoruz

                ProductName = x.Product != null ? x.Product.ProductName : "Tanımsız Ürün",
                Barcode = x.Product != null ? x.Product.Barcode : "-",

                CategoryName = x.Product != null && x.Product.Category != null
                    ? x.Product.Category.CategoryName
                    : "-",

                UnitTypeName = x.Product != null && x.Product.UnitType != null
                    ? x.Product.UnitType.UnitName
                    : "-",

                CompanyName = x.Company != null ? x.Company.CompanyName : "-",
                IsActive = x.IsActive

            }).ToList();
        }
    }
}
