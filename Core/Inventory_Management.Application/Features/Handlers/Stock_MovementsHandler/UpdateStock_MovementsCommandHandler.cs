using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
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
            var val = await _context.Stock_Movements.FindAsync(request.Id);
            val.UserId = request.UserId;
            val.Quantity = request.Quantity;
            val.MoveTypeId = request.MoveTypeId;
            val.InventoryId = request.InventoryId;
            val.SupplierId = request.SupplierId;
            val.Payment = request.Payment;
            val.Description = request.Description;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}