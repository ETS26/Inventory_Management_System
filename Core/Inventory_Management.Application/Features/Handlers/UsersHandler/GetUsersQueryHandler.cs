using Inventory_Management.Application.Features.Queries.UsersQuery;
using Inventory_Management.Application.Features.Results.UsersResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UsersHandler
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<GetUsersQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetUsersQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetUsersQueryResult>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Users.ToListAsync();
            return val.Select(x => new GetUsersQueryResult
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                PhoneNumber = x.PhoneNumber,
                Password = x.Password,
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}