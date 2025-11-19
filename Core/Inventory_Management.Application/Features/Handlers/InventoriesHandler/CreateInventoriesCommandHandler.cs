using Inventory_Management.Application.Features.Commands.InventoriesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.InventoriesHandler
{
    public class CreateInventoriesCommandHandler : IRequestHandler<CreateInventoriesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateInventoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateInventoriesCommand request, CancellationToken cancellationToken)
        {
            await _context.Inventories.AddAsync(new Inventories
            {
                CompanyId = request.CompanyId,
                BatchNumber = request.BatchNumber,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CriticalStockQuantity = request.CriticalStockQuantity,
                PurchasePrice = request.PurchasePrice,
                SalePrice = request.SalePrice,
                ExpirationDate = request.ExpirationDate,
                Description = request.Description,
            });
            await _context.SaveChangesAsync();
            
        }
    }
}