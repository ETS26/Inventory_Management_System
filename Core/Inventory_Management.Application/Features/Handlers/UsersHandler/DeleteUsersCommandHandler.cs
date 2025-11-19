using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.UsersCommand;

namespace Inventory_Management.Application.Features.Handlers.UsersHandler
{
    public class DeleteUsersCommandHandler : IRequestHandler<DeleteUsersCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteUsersCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteUsersCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Users.FindAsync(request.Id);
            _context.Users.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}