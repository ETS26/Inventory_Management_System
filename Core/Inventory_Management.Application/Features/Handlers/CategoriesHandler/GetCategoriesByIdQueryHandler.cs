
using Inventory_Management.Application.Features.Queries.CategoriesQuery;
using Inventory_Management.Application.Features.Results.CategoriesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CategoriesHandler
{
    public class GetCategoriesByIdQueryHandler : IRequestHandler<GetCategoriesByIdQuery, GetCategoriesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetCategoriesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetCategoriesByIdQueryResult> Handle(GetCategoriesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Categories.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetCategoriesByIdQueryResult
            {
                Id = val.Id,
                CategoryName = val.CategoryName,
                Description = val.Description,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}
