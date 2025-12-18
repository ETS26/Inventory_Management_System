using Inventory_Management.Application.Features.Commands.SuppliersCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class ActivateSupplierCommandHandler : IRequestHandler<ActivateSupplierCommand>
    {
        private readonly Inventory_Management_Context _context;

        public ActivateSupplierCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(ActivateSupplierCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _context.Suppliers.FindAsync(request.Id);
            if (supplier != null)
            {
                supplier.IsActive = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
