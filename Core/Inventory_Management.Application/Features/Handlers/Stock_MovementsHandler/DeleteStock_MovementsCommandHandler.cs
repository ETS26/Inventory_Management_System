using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.Stock_MovementsCommand;

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
            var val = await _context.Stock_Movements.FindAsync(request.Id);
            _context.Stock_Movements.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}