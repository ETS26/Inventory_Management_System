using Inventory_Management.Application.Features.Queries.Move_TypesQuery;
using Inventory_Management.Application.Features.Results.Move_TypesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Move_TypesHandler
{
    public class GetMove_TypesByIdQueryHandler : IRequestHandler<GetMove_TypesByIdQuery, GetMove_TypesByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetMove_TypesByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetMove_TypesByIdQueryResult> Handle(GetMove_TypesByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Move_Types.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetMove_TypesByIdQueryResult
            {
                Id = val.Id,
                MoveType = val.MoveType,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}