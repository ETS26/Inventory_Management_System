using Inventory_Management.Application.Features.Commands.UsersCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using BCrypt.Net;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UsersHandler
{
    public class CreateUsersCommandHandler : IRequestHandler<CreateUsersCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateUsersCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateUsersCommand request, CancellationToken cancellationToken)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _context.Users.AddAsync(new Users
            {
                CompanyId = request.CompanyId,
                PhoneNumber = request.PhoneNumber,
                Password = hashedPassword,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            });
            await _context.SaveChangesAsync();
            
        }
    }
}