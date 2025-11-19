using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.InventoriesCommand;

namespace Inventory_Management.Application.Features.Handlers.InventoriesHandler
{
    public class DeleteInventoriesCommandHandler : IRequestHandler<DeleteInventoriesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteInventoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteInventoriesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Inventories.FindAsync(request.Id);
            _context.Inventories.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}