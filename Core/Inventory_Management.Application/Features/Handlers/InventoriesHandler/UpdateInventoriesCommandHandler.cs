using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
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
            var val = await _context.Inventories.FindAsync(request.Id);
            val.BatchNumber = request.BatchNumber;
            val.ExpirationDate = request.ExpirationDate;
            val.Quantity = request.Quantity;
            val.CriticalStockQuantity = request.CriticalStockQuantity;
            val.PurchasePrice = request.PurchasePrice;
            val.SalePrice = request.SalePrice;
            val.ProductId = request.ProductId;
            val.CompanyId = request.CompanyId;
            val.Description = request.Description;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}