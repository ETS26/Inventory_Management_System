using Inventory_Management.Application.Features.Commands.SuppliersCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class CreateSuppliersCommandHandler : IRequestHandler<CreateSuppliersCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateSuppliersCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateSuppliersCommand request, CancellationToken cancellationToken)
        {
            await _context.Suppliers.AddAsync(new Suppliers
            {
                SupplierName = request.SupplierName,
                ContactPerson = request.ContactPerson,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email
            });
            await _context.SaveChangesAsync();
            
        }
    }
}