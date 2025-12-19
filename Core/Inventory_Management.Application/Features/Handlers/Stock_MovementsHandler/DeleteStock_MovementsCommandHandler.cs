using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Microsoft.EntityFrameworkCore;
using Inventory_Management.Application.Features.Exceptions;

namespace Inventory_Management.Application.Features.Handlers.Stock_MovementsHandler
{
    public class DeleteStock_MovementsCommandHandler : IRequestHandler<DeleteStock_MovementsCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteStock_MovementsCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteStock_MovementsCommand request, CancellationToken cancellationToken)
        {
            // Hareket, hareket tipi ve envanter bilgilerini birlikte çek
            var stockMovement = await _context.Stock_Movements
                                              .Include(sm => sm.MoveType)
                                              .FirstOrDefaultAsync(sm => sm.Id == request.Id, cancellationToken);

            if (stockMovement == null)
            {
                throw new NotFoundException("Silinecek stok hareketi bulunamadı.");
            }

            if (stockMovement.InventoryId != Guid.Empty)
            {
                var inventory = await _context.Inventories.FindAsync(stockMovement.InventoryId);
                if (inventory != null)
                {
                    // Hareket tipinin adına göre işlem yap (Case-insensitive)
                    var moveTypeName = stockMovement.MoveType?.MoveType?.ToLower() ?? "";
                    if (moveTypeName.Contains("giriş") || moveTypeName.Contains("in") || moveTypeName.Contains("income"))
                    {
                        // Eğer hareket bir 'giriş' ise, silindiğinde envanterden düşülür.
                        inventory.Quantity -= stockMovement.Quantity;
                    }
                    else if (moveTypeName.Contains("çıkış") || moveTypeName.Contains("out") || moveTypeName.Contains("outcome"))
                    {
                        // Eğer hareket bir 'çıkış' ise, silindiğinde envantere geri eklenir.
                        inventory.Quantity += stockMovement.Quantity;
                    }
                    _context.Inventories.Update(inventory);
                }
            }
            
            // Hareketi hard delete yerine soft delete yap
            stockMovement.IsActive = false;
            _context.Stock_Movements.Update(stockMovement);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}