
using Inventory_Management.Application.Features.Commands.UsersRolesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UserRolesHandler
{
    public class CreateUsersRolesCommandHandler : IRequestHandler<CreateUsersRolesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateUsersRolesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateUsersRolesCommand request, CancellationToken cancellationToken)
        {
            await _context.UsersRoles.AddAsync(new UsersRoles
            {
                UserId = request.UserId,
                RoleId = request.RoleId
            });
            await _context.SaveChangesAsync();
            
        }
    }
}
