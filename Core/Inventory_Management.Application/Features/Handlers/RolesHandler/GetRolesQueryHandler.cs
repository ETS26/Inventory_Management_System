using Inventory_Management.Application.Features.Queries.RolesQuery;
using Inventory_Management.Application.Features.Results.RolesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.RolesHandler
{
    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<GetRolesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetRolesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetRolesQueryResult>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Roles.ToListAsync();
            return val.Select(x => new GetRolesQueryResult
            {
                Id = x.Id,
                RoleName = x.RoleName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}