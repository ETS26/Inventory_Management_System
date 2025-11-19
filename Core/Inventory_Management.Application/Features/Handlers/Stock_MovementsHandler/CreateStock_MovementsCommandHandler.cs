using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
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
            await _context.Stock_Movements.AddAsync(new Stock_Movements
            {  
                InventoryId = request.InventoryId,
                MoveTypeId = request.MoveTypeId,
                UserId = request.UserId,
                SupplierId = request.SupplierId,
                Quantity = request.Quantity,
                Payment = request.Payment,
                Description = request.Description
            });
            await _context.SaveChangesAsync();
        }
    }
}
