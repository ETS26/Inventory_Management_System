using Inventory_Management.Application.Features.Commands.CompaniesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CompaniesHandler
{
    public class UpdateCompaniesCommandHandler : IRequestHandler<UpdateCompaniesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateCompaniesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateCompaniesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Companies.FindAsync(request.Id);
            val.CompanyName = request.CompanyName;
            val.Address = request.Address;
            val.PhoneNumber = request.PhoneNumber;
            val.Email = request.Email;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}