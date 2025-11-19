using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.Move_TypesCommand;

namespace Inventory_Management.Application.Features.Handlers.Move_TypesHandler
{
    public class DeleteMove_TypesCommandHandler : IRequestHandler<DeleteMove_TypesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteMove_TypesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteMove_TypesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Move_Types.FindAsync(request.Id);
            _context.Move_Types.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}