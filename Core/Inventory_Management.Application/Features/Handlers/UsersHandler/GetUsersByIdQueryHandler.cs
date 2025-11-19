using Inventory_Management.Application.Features.Queries.UsersQuery;
using Inventory_Management.Application.Features.Results.UsersResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.UsersHandler
{
    public class GetUsersByIdQueryHandler : IRequestHandler<GetUsersByIdQuery, GetUsersByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetUsersByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetUsersByIdQueryResult> Handle(GetUsersByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Users.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetUsersByIdQueryResult
            {
                Id = val.Id,
                CompanyId = val.CompanyId,
                PhoneNumber = val.PhoneNumber,
                Password = val.Password,
                Email = val.Email,
                FirstName = val.FirstName,
                LastName = val.LastName,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}