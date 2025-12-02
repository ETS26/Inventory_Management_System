using Inventory_Management.Application.Features.Commands.SuppliersCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class UpdateSuppliersCommandHandler : IRequestHandler<UpdateSuppliersCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateSuppliersCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateSuppliersCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Suppliers.FindAsync(request.Id);
            val.CompanyId = request.CompanyId;
            val.SupplierName = request.SupplierName;
            val.ContactPerson = request.ContactPerson;
            val.Address = request.Address;
            val.PhoneNumber = request.PhoneNumber;
            val.Email = request.Email;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}