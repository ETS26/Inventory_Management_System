
using Inventory_Management.Application.Features.Queries.UserRolesQuery;
using Inventory_Management.Application.Features.Results.UserRolesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UserRolesHandler
{
    public class GetUsersRolesByIdQueryHandler : IRequestHandler<GetUserRolesByIdQuery, GetUserRolesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetUsersRolesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetUserRolesByIdQueryResult> Handle(GetUserRolesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.UsersRoles.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetUserRolesByIdQueryResult
            {
                Id = val.Id,
                UserId = val.UserId,
                RoleId = val.RoleId,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}
