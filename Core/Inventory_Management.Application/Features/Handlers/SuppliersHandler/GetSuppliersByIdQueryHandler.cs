using Inventory_Management.Application.Features.Queries.SuppliersQuery;
using Inventory_Management.Application.Features.Results.SuppliersResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class GetSuppliersByIdQueryHandler : IRequestHandler<GetSuppliersByIdQuery, GetSuppliersByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetSuppliersByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetSuppliersByIdQueryResult> Handle(GetSuppliersByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Suppliers.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetSuppliersByIdQueryResult
            {
                Id = val.Id,
                SupplierName = val.SupplierName,
                ContactPerson = val.ContactPerson,
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