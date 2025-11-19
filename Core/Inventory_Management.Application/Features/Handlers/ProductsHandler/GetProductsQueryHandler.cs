using Inventory_Management.Application.Features.Queries.ProductsQuery;
using Inventory_Management.Application.Features.Results.ProductsResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.ProductsHandler
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<GetProductsQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetProductsQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetProductsQueryResult>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Products.ToListAsync();
            return val.Select(x => new GetProductsQueryResult
            {
                Id = x.Id,
                ProductName = x.ProductName,
                Description = x.Description,
                Barcode = x.Barcode,
                CategoryId = x.CategoryId,
                UnitTypeId = x.UnitTypeId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}