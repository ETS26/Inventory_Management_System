using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore; // Bu kütüphaneyi eklemeyi unutmayın!
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.InventoriesHandler
{
    public class SoftDeleteInventoriesCommandHandler : IRequestHandler<SoftDeleteInventoriesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public SoftDeleteInventoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(SoftDeleteInventoriesCommand request, CancellationToken cancellationToken)
        {
            // 1. Envanter kaydını bul
            var inventoryItem = await _context.Inventories.FindAsync(new object[] { request.Id }, cancellationToken);

            if (inventoryItem == null)
            {
                throw new Exception("Pasifleştirilecek envanter kaydı bulunamadı.");
            }

            // 2. Envanter kaydını pasifleştir
            inventoryItem.IsActive = false;
            inventoryItem.UpdatedAt = DateTime.UtcNow;

            // 3. İlgili Stok Hareketlerini bul ve onları da pasifleştir
            var relatedMovements = await _context.Stock_Movements
                .Where(sm => sm.InventoryId == inventoryItem.Id && sm.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var movement in relatedMovements)
            {
                movement.IsActive = false;
                movement.UpdatedAt = DateTime.UtcNow;
            }

            // 4. Tüm değişiklikleri tek bir işlemde veritabanına kaydet
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}