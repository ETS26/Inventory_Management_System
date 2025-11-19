using Inventory_Management.Application.Features.Queries.CompaniesQuery;
using Inventory_Management.Application.Features.Results.CompaniesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CompaniesHandler
{
    public class GetCompaniesByIdQueryHandler : IRequestHandler<GetCompaniesByIdQuery, GetCompaniesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetCompaniesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetCompaniesByIdQueryResult> Handle(GetCompaniesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Companies.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetCompaniesByIdQueryResult
            {
                Id = val.Id,
                CompanyName = val.CompanyName,
                Address = val.Address,
                PhoneNumber = val.PhoneNumber,
                Email = val.Email,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}