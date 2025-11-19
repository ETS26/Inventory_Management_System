using Inventory_Management.Application.Features.Commands.UsersCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UsersHandler
{
    public class UpdateUsersCommandHandler : IRequestHandler<UpdateUsersCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateUsersCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateUsersCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Users.FindAsync(request.Id);
            val.CompanyId = request.CompanyId;
            val.PhoneNumber = request.PhoneNumber;
            val.Password = request.Password;
            val.Email = request.Email;
            val.FirstName = request.FirstName;
            val.LastName = request.LastName;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}