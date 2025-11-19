using Inventory_Management.Application.Features.Commands.Suppliers_DeliveryCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Suppliers_DeliveryHandler
{
    public class CreateSuppliers_DeliveryCommandHandler : IRequestHandler<CreateSuppliers_DeliveryCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateSuppliers_DeliveryCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateSuppliers_DeliveryCommand request, CancellationToken cancellationToken)
        {
            await _context.Suppliers_Deliveries.AddAsync(new Suppliers_Delivery
            {
                SupplierId = request.SupplierId,
                RuleId = request.RuleId,
                Description = request.Description
            });
            await _context.SaveChangesAsync();
            
        }
    }
}