using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Inventory_Management.Application.Features.Exceptions;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Stock_MovementsHandler
{
    public class UpdateStock_MovementsCommandHandler : IRequestHandler<UpdateStock_MovementsCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateStock_MovementsCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(UpdateStock_MovementsCommand request, CancellationToken cancellationToken)
        {
            // 1. Orijinal hareketi "AsNoTracking" ile alarak mevcut durumun bir kopyasını oluştur.
            var originalMovement = await _context.Stock_Movements
                                                 .Include(sm => sm.MoveType)
                                                 .AsNoTracking()
                                                 .FirstOrDefaultAsync(sm => sm.Id == request.Id, cancellationToken);

            if (originalMovement == null)
            {
                throw new NotFoundException("Güncellenecek stok hareketi bulunamadı.");
            }

            // 2. Güncellenecek asıl hareketi context'e izlet (track et).
            var stockMovementToUpdate = await _context.Stock_Movements
                                                      .FirstOrDefaultAsync(sm => sm.Id == request.Id, cancellationToken);
            if(stockMovementToUpdate == null)
            {
                throw new NotFoundException("Güncellenecek stok hareketi bulunamadı (izleme).");
            }

            // 3. Eski hareketi geri al.
            if (originalMovement.InventoryId != Guid.Empty)
            {
                var originalInventory = await _context.Inventories.FindAsync(originalMovement.InventoryId);
                if (originalInventory != null)
                {
                    var originalMoveTypeName = originalMovement.MoveType?.MoveType?.ToLower() ?? "";
                    if (originalMoveTypeName.Contains("giriş") || originalMoveTypeName.Contains("in"))
                    {
                        originalInventory.Quantity -= originalMovement.Quantity;
                    }
                    else if (originalMoveTypeName.Contains("çıkış") || originalMoveTypeName.Contains("out"))
                    {
                        originalInventory.Quantity += originalMovement.Quantity;
                    }
                    _context.Inventories.Update(originalInventory);
                }
            }

            // 4. Yeni hareketi uygula
            if (request.InventoryId != Guid.Empty)
            {
                var newInventory = await _context.Inventories.FindAsync(request.InventoryId);
                if (newInventory == null)
                {
                    throw new NotFoundException("Yeni envanter öğesi bulunamadı.");
                }

                var newMoveType = await _context.Move_Types.FindAsync(request.MoveTypeId);
                if (newMoveType == null)
                {
                    throw new NotFoundException("Yeni hareket tipi bulunamadı.");
                }

                var newMoveTypeName = newMoveType.MoveType.ToLower();
                if (newMoveTypeName.Contains("çıkış") || newMoveTypeName.Contains("out"))
                {
                    // Eğer yeni hareket bir çıkışsa, yeterli stok olup olmadığını kontrol et.
                    // Not: Bu kontrol, eski hareket geri alındıktan sonraki envanter miktarına göre yapılır.
                    if (newInventory.Quantity < request.Quantity)
                    {
                        throw new BadRequestException($"Yetersiz Stok. Mevcut: {newInventory.Quantity}, Talep Edilen: {request.Quantity}");
                    }
                    newInventory.Quantity -= request.Quantity;
                }
                else if (newMoveTypeName.Contains("giriş") || newMoveTypeName.Contains("in"))
                {
                    newInventory.Quantity += request.Quantity;
                }
                 _context.Inventories.Update(newInventory);
            }

            // 5. Stok hareketinin kendisini güncelle.
            stockMovementToUpdate.UserId = request.UserId;
            stockMovementToUpdate.Quantity = request.Quantity;
            stockMovementToUpdate.MoveTypeId = request.MoveTypeId;
            stockMovementToUpdate.InventoryId = request.InventoryId;
            stockMovementToUpdate.SupplierId = request.SupplierId;
            stockMovementToUpdate.Description = request.Description;
            stockMovementToUpdate.UpdatedAt = DateTime.UtcNow;

            _context.Stock_Movements.Update(stockMovementToUpdate);

            // 6. Tüm değişiklikleri tek bir transaction'da kaydet.
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}