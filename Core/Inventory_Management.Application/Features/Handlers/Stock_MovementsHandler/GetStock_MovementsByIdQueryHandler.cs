using Inventory_Management.Application.Features.Queries.Stock_MovementsQuery;
using Inventory_Management.Application.Features.Results.Stock_MovementsResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Stock_MovementsHandler
{
    public class GetStock_MovementsByIdQueryHandler : IRequestHandler<GetStock_MovementsByIdQuery, GetStock_MovementsByIdQueryResult>
    {
        private readonly Inventory_Management_Context _context;
        public GetStock_MovementsByIdQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<GetStock_MovementsByIdQueryResult> Handle(GetStock_MovementsByIdQuery request, CancellationToken cancellationToken)
        {
           var val = await _context.Stock_Movements.FindAsync(request.Id);
            if (val == null) { return null; }
            return new GetStock_MovementsByIdQueryResult
            {
                Id = val.Id,
                UserId = val.UserId,
                Quantity = val.Quantity,
                MoveTypeId = val.MoveTypeId,
                InventoryId = val.InventoryId,
                SupplierId = val.SupplierId,
                Payment = val.Payment,
                Description = val.Description,
                CreatedAt = val.CreatedAt,
                UpdatedAt = val.UpdatedAt,
                IsActive = val.IsActive

            };
        }
    }
}