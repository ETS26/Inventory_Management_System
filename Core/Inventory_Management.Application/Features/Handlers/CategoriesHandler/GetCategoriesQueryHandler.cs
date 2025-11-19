
using Inventory_Management.Application.Features.Queries.CategoriesQuery;
using Inventory_Management.Application.Features.Results.CategoriesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CategoriesHandler
{
    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<GetCategoriesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetCategoriesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetCategoriesQueryResult>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Categories.ToListAsync();
            return val.Select(x => new GetCategoriesQueryResult
            {
                Id = x.Id,
                CategoryName = x.CategoryName,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}
