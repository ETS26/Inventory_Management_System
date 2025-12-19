using Inventory_Management.Application.Features.Queries.Stock_MovementsQuery;
using Inventory_Management.Application.Features.Results.Stock_MovementsResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Stock_MovementsHandler
{
    public class GetStock_MovementsQueryHandler : IRequestHandler<GetStock_MovementsQuery, List<GetStock_MovementsQueryResult>>
    {
        private readonly Inventory_Management_Context _context;

        public GetStock_MovementsQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task<List<GetStock_MovementsQueryResult>> Handle(GetStock_MovementsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Stock_Movements.AsNoTracking();

            // Eğer IsActive filtresi request içinde belirtilmişse, sorguya Where koşulu ekle
            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            // 1. ADIM: Veritabanından Ham Veriyi Çek (Sadece Entity Listesi)
            // Burada Select veya hesaplama YAPMIYORUZ. Sadece veriyi alıyoruz.
            var movements = await query
                .Include(x => x.Inventory).ThenInclude(i => i.Product).ThenInclude(p => p.UnitType)
                .Include(x => x.MoveType)
                .Include(x => x.User)
                .Include(x => x.Supplier)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            // 2. ADIM: Hafızada DTO'ya Dönüştür (Mapping)
            // Veri artık RAM'de olduğu için burada istediğimiz hesaplamayı hatasız yaparız.

            var resultList = new List<GetStock_MovementsQueryResult>();

            foreach (var item in movements)
            {
                // Fiyat Hesaplama
                float unitPrice = 0;
                string typeName = (item.MoveType?.MoveType ?? "").ToLower();

                // Stok aktif değilse bile fiyat göstermeye çalış
                var inventoryItem = item.Inventory;

                if (typeName.Contains("stock in") || typeName.Contains("giriş") || typeName.Contains("in"))
                {
                    unitPrice = inventoryItem?.PurchasePrice ?? 0;
                }
                else
                {
                    unitPrice = inventoryItem?.SalePrice ?? 0;
                }

                // Listeye Ekle (DTO Oluştur)
                resultList.Add(new GetStock_MovementsQueryResult
                {
                    // ID'ler
                    Id = item.Id,
                    InventoryId = item.InventoryId,
                    MoveTypeId = item.MoveTypeId,
                    SupplierId = item.SupplierId,
                    UserId = item.UserId,

                    // Diğer Bilgiler
                    IsActive = item.IsActive, 
                    CreatedAt = item.CreatedAt,
                    ProductName = inventoryItem?.Product?.ProductName ?? "Silinmiş Ürün",
                    MoveTypeName = item.MoveType?.MoveType ?? "-",
                    UnitTypeName = inventoryItem?.Product?.UnitType?.UnitName ?? "-",
                    UserName = item.User != null ? $"{item.User.FirstName} {item.User.LastName}" : "Bilinmiyor",
                    SupplierName = item.Supplier?.SupplierName ?? "-",
                    BatchNumber = inventoryItem?.BatchNumber,
                    ExpirationDate = inventoryItem?.ExpirationDate,
                    Quantity = item.Quantity,
                    Description = item.Description ?? "",
                    Payment = item.Quantity * unitPrice
                });
            }

            // 3. ADIM: Güvenli Listeyi Döndür
            return resultList;
        }
    }
}