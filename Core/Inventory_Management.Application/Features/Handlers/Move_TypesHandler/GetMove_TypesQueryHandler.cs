using Inventory_Management.Application.Features.Queries.Move_TypesQuery;
using Inventory_Management.Application.Features.Results.Move_TypesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Move_TypesHandler
{
    public class GetMove_TypesQueryHandler : IRequestHandler<GetMove_TypesQuery, List<GetMove_TypesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetMove_TypesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetMove_TypesQueryResult>> Handle(GetMove_TypesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Move_Types.ToListAsync();
            return val.Select(x => new GetMove_TypesQueryResult
            {
                Id = x.Id,
                MoveType = x.MoveType,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}