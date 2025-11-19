
using Inventory_Management.Application.Features.Queries.UserRolesQuery;
using Inventory_Management.Application.Features.Results.UserRolesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UserRolesHandler
{
    public class GetUsersRolesQueryHandler : IRequestHandler<GetUserRolesQuery, List<GetUserRolesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetUsersRolesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetUserRolesQueryResult>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.UsersRoles.ToListAsync();
            return val.Select(x => new GetUserRolesQueryResult
            {
                Id = x.Id,
                UserId = x.UserId,
                RoleId = x.RoleId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}
