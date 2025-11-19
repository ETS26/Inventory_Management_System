using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.RolesCommand;

namespace Inventory_Management.Application.Features.Handlers.RolesHandler
{
    public class DeleteRolesCommandHandler : IRequestHandler<DeleteRolesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteRolesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteRolesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Roles.FindAsync(request.Id);
            _context.Roles.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}