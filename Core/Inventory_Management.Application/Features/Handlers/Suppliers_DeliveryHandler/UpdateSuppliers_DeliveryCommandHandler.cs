using Inventory_Management.Application.Features.Commands.Suppliers_DeliveryCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Suppliers_DeliveryHandler
{
    public class UpdateSuppliers_DeliveryCommandHandler : IRequestHandler<UpdateSuppliers_DeliveryCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateSuppliers_DeliveryCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateSuppliers_DeliveryCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Suppliers_Deliveries.FindAsync(request.Id);
            val.CompanyId = request.CompanyId;
            val.SupplierId = request.SupplierId;
            val.RuleId = request.RuleId;
            val.Description = request.Description;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}