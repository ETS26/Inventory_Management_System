using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.Suppliers_DeliveryCommand;

namespace Inventory_Management.Application.Features.Handlers.Suppliers_DeliveryHandler
{
    public class DeleteSuppliers_DeliveryCommandHandler : IRequestHandler<DeleteSuppliers_DeliveryCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteSuppliers_DeliveryCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteSuppliers_DeliveryCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Suppliers_Deliveries.FindAsync(request.Id);
            _context.Suppliers_Deliveries.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}