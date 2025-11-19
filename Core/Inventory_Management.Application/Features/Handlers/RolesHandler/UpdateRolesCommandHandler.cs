using Inventory_Management.Application.Features.Commands.RolesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.RolesHandler
{
    public class UpdateRolesCommandHandler : IRequestHandler<UpdateRolesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateRolesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateRolesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Roles.FindAsync(request.Id);
            val.RoleName = request.RoleName;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}