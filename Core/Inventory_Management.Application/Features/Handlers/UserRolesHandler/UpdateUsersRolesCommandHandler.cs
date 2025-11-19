
using Inventory_Management.Application.Features.Commands.UsersRolesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UserRolesHandler
{
    public class UpdateUsersRolesCommandHandler : IRequestHandler<UpdateUsersRolesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateUsersRolesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateUsersRolesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.UsersRoles.FindAsync(request.Id);
            val.UserId = request.UserId;
            val.RoleId = request.RoleId;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
