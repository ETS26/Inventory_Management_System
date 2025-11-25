using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Stock_MovementsHandler
{
    public class CreateStock_MovementsCommandHandler : IRequestHandler<CreateStock_MovementsCommand>
    {
        private readonly Inventory_Management_Context _context;
        public CreateStock_MovementsCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(CreateStock_MovementsCommand request, CancellationToken cancellationToken)
        {
            // 1. Ýlgili Envanter Kaydýný Bul
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(x => x.Id == request.InventoryId, cancellationToken);

            if (inventory == null)
                throw new Exception("Hata: Ýlgili envanter kaydý bulunamadý!");

            // 2. Hareket Tipini Bul (Giriþ mi, Çýkýþ mý?)
            var moveType = await _context.Move_Types
                .FirstOrDefaultAsync(x => x.Id == request.MoveTypeId, cancellationToken);

            if (moveType == null)
                throw new Exception("Hata: Geçersiz hareket tipi!");

            // 3. Stok Miktarýný Güncelle (Mantýk Kýsmý)
            // NOT: Veritabanýnýzda MoveType isimlerinin "Income" (Giriþ) ve "Outcome" (Çýkýþ) 
            // veya "Stock In" / "Stock Out" olarak kayýtlý olduðunu varsayýyoruz.
            // Bunu kendi veritabanýnýzdaki isimlere göre düzeltebilirsiniz.

            bool isIncome = moveType.MoveType.ToLower().Contains("stock in") || moveType.MoveType.ToLower().Contains("giriþ") || moveType.MoveType.ToLower().Contains("in");
            bool isOutcome = moveType.MoveType.ToLower().Contains("stock out") || moveType.MoveType.ToLower().Contains("çýkýþ") || moveType.MoveType.ToLower().Contains("out");

            if (isIncome)
            {
                // Stok GÝRÝÞÝ: Miktarý artýr
                inventory.Quantity += request.Quantity;
            }
            else if (isOutcome)
            {
                // Stok ÇIKIÞI: Miktarý azalt (Önce yeterli stok var mý kontrol et)
                if (inventory.Quantity < request.Quantity)
                {
                    throw new Exception($"Yetersiz Stok! Mevcut: {inventory.Quantity}, Ýstenen Çýkýþ: {request.Quantity}");
                }
                inventory.Quantity -= request.Quantity;
            }

            // 4. Stok Hareketini Kayýt Ýçin Hazýrla
            var movement = new Stock_Movements
            {
                InventoryId = request.InventoryId,
                MoveTypeId = request.MoveTypeId,
                CompanyId = inventory.CompanyId, // Envanterin ait olduðu þirkete kaydet
                UserId = request.UserId, // Login olan kullanýcýnýn ID'si (Frontend'den gelecek)
                SupplierId = request.SupplierId, // Opsiyonel olabilir
                Quantity = request.Quantity,
                Description = request.Description,
               
            };

            // 5. Her Ýki Ýþlemi de Kaydet (Transaction)
            await _context.Stock_Movements.AddAsync(movement, cancellationToken);
            _context.Inventories.Update(inventory); // Envanter güncellendi olarak iþaretle

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
