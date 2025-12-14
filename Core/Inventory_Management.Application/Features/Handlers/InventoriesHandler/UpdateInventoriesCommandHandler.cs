using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.InventoriesHandler
{
    public class UpdateInventoriesCommandHandler : IRequestHandler<UpdateInventoriesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateInventoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateInventoriesCommand request, CancellationToken cancellationToken)
        {
            var inventoryItem = await _context.Inventories.FindAsync(new object[] { request.Id }, cancellationToken);
            if (inventoryItem == null)
            {
                throw new Exception($"Inventory item with Id {request.Id} not found.");
            }

            var originalIsActive = inventoryItem.IsActive;

            // Update inventory item properties from the request
            inventoryItem.BatchNumber = request.BatchNumber;
            inventoryItem.ExpirationDate = request.ExpirationDate;
            inventoryItem.Quantity = request.Quantity;
            inventoryItem.CriticalStockQuantity = request.CriticalStockQuantity;
            inventoryItem.PurchasePrice = request.PurchasePrice;
            inventoryItem.SalePrice = request.SalePrice;
            inventoryItem.Description = request.Description;
            inventoryItem.IsActive = request.IsActive;
            inventoryItem.UpdatedAt = DateTime.UtcNow;

            // If the IsActive status was flipped, cascade the change using a direct database update.
            if (originalIsActive != inventoryItem.IsActive)
            {
                await _context.Stock_Movements
                    .Where(sm => sm.InventoryId == inventoryItem.Id && sm.IsActive != inventoryItem.IsActive)
                    .ExecuteUpdateAsync(updates => updates
                        .SetProperty(m => m.IsActive, inventoryItem.IsActive)
                        .SetProperty(m => m.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);
            }

            // Save the changes to the inventoryItem itself.
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}