using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.SuppliersCommand;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class DeleteSuppliersCommandHandler : IRequestHandler<DeleteSuppliersCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteSuppliersCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteSuppliersCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Suppliers.FindAsync(request.Id);
            _context.Suppliers.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}