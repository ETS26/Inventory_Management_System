using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.CompaniesCommand;

namespace Inventory_Management.Application.Features.Handlers.CompaniesHandler
{
    public class DeleteCompaniesCommandHandler : IRequestHandler<DeleteCompaniesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteCompaniesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteCompaniesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Companies.FindAsync(request.Id);
            _context.Companies.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}