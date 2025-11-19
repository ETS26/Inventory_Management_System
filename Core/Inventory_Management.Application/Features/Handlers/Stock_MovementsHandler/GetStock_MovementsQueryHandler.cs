using Inventory_Management.Application.Features.Queries.Stock_MovementsQuery;
using Inventory_Management.Application.Features.Results.Stock_MovementsResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Stock_MovementsHandler
{
    public class GetStock_MovementsQueryHandler : IRequestHandler<GetStock_MovementsQuery, List<GetStock_MovementsQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetStock_MovementsQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetStock_MovementsQueryResult>> Handle(GetStock_MovementsQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Stock_Movements.ToListAsync();
            return val.Select(x => new GetStock_MovementsQueryResult
            {
                Id = x.Id,
                UserId = x.UserId,
                Payment = x.Payment,
                Quantity = x.Quantity,
                MoveTypeId = x.MoveTypeId,
                InventoryId = x.InventoryId,
                SupplierId = x.SupplierId,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}