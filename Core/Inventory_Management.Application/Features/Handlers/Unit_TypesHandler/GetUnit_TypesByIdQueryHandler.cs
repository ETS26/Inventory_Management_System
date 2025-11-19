using Inventory_Management.Application.Features.Queries.Unit_TypesQuery;
using Inventory_Management.Application.Features.Results.Unit_TypesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Unit_TypesHandler
{
    public class GetUnit_TypesByIdQueryHandler : IRequestHandler<GetUnit_TypesByIdQuery, GetUnit_TypesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetUnit_TypesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetUnit_TypesByIdQueryResult> Handle(GetUnit_TypesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Unit_Types.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetUnit_TypesByIdQueryResult
            {
                Id = val.Id,
                UnitName = val.UnitName,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}