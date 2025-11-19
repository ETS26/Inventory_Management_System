using Inventory_Management.Application.Features.Queries.CompaniesQuery;
using Inventory_Management.Application.Features.Results.CompaniesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CompaniesHandler
{
    public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, List<GetCompaniesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetCompaniesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetCompaniesQueryResult>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Companies.ToListAsync();
            return val.Select(x => new GetCompaniesQueryResult
            {
                Id = x.Id,
                CompanyName = x.CompanyName,
                Address = x.Address,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}