using Inventory_Management.Application.Features.Commands.RolesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.RolesHandler
{
    public class CreateRolesCommandHandler : IRequestHandler<CreateRolesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateRolesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateRolesCommand request, CancellationToken cancellationToken)
        {
            await _context.Roles.AddAsync(new Roles
            {
                RoleName = request.RoleName
            });
            await _context.SaveChangesAsync();
            
        }
    }
}