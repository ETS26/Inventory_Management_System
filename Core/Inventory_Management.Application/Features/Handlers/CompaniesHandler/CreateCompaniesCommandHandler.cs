using Inventory_Management.Application.Features.Commands.CompaniesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CompaniesHandler
{
    public class CreateCompaniesCommandHandler : IRequestHandler<CreateCompaniesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateCompaniesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateCompaniesCommand request, CancellationToken cancellationToken)
        {
            await _context.Companies.AddAsync(new Companies
            {
                CompanyName = request.CompanyName,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email
            });
            await _context.SaveChangesAsync();
            
        }
    }
}