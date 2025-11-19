using Inventory_Management.Application.Features.Queries.SuppliersQuery;
using Inventory_Management.Application.Features.Results.SuppliersResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<GetSuppliersQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetSuppliersQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetSuppliersQueryResult>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Suppliers.ToListAsync();
            return val.Select(x => new GetSuppliersQueryResult
            {
                Id = x.Id,
                SupplierName = x.SupplierName,
                ContactPerson = x.ContactPerson,
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