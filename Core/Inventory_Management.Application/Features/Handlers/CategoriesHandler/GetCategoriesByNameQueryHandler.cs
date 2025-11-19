using Inventory_Management.Application.Features.Queries.CategoriesQuery;
using Inventory_Management.Application.Features.Results.CategoriesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CategoriesHandler
{
    public class GetCategoriesByNameQueryHandler : IRequestHandler<GetCategoriesByNameQuery, GetCategoriesByNameQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetCategoriesByNameQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetCategoriesByNameQueryResult> Handle(GetCategoriesByNameQuery request, CancellationToken cancellationToken)
        {
            // 1. FindAsync YERİNE FirstOrDefaultAsync KULLANILDI
            // "c => c.CategoryName == request.CategoryName" koşuluna uyan ilk kaydı getirir.
            var val = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == request.CategoryName, cancellationToken);

            // 2. ÇOK ÖNEMLİ: Null kontrolü eklendi.
            // Eğer o isimde bir kategori bulunamazsa 'val' null olur.
            if (val == null)
            {
                // Bulunamazsa null dönebilirsin veya bir hata fırlatabilirsin.
                // Şimdilik null dönelim. API controller'da bu null'ı "Not Found" (404) olarak yönetmelisin.
                return null;
            }
            return new GetCategoriesByNameQueryResult
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
