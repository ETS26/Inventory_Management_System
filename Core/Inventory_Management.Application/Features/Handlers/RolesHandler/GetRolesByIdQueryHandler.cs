using Inventory_Management.Application.Features.Queries.RolesQuery;
using Inventory_Management.Application.Features.Results.RolesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.RolesHandler
{
    public class GetRolesByIdQueryHandler : IRequestHandler<GetRolesByIdQuery, GetRolesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetRolesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetRolesByIdQueryResult> Handle(GetRolesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Roles.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetRolesByIdQueryResult
            {
                Id = val.Id,
                RoleName = val.RoleName,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}