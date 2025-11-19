using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.Unit_TypesCommand;

namespace Inventory_Management.Application.Features.Handlers.Unit_TypesHandler
{
    public class DeleteUnit_TypesCommandHandler : IRequestHandler<DeleteUnit_TypesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteUnit_TypesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteUnit_TypesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Unit_Types.FindAsync(request.Id);
            _context.Unit_Types.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}