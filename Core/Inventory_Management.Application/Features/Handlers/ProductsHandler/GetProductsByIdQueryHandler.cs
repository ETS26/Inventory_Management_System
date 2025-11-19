using Inventory_Management.Application.Features.Queries.ProductsQuery;
using Inventory_Management.Application.Features.Results.ProductsResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.ProductsHandler
{
    public class GetProductsByIdQueryHandler : IRequestHandler<GetProductsByIdQuery, GetProductsByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetProductsByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetProductsByIdQueryResult> Handle(GetProductsByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Products.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetProductsByIdQueryResult
            {
                Id = val.Id,
                ProductName = val.ProductName,
                Description = val.Description,
                Barcode = val.Barcode,
                CategoryId = val.CategoryId,
                UnitTypeId = val.UnitTypeId,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}