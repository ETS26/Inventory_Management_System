
using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.UsersRolesCommand;

namespace Inventory_Management.Application.Features.Handlers.UserRolesHandler
{
    public class DeleteUsersRolesCommandHandler : IRequestHandler<DeleteUsersRolesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteUsersRolesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteUsersRolesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.UsersRoles.FindAsync(request.Id);
            _context.UsersRoles.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}
