using Inventory_Management.Application.Features.Queries.Unit_TypesQuery;
using Inventory_Management.Application.Features.Results.Unit_TypesResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Unit_TypesHandler
{
    public class GetUnit_TypesQueryHandler : IRequestHandler<GetUnit_TypesQuery, List<GetUnit_TypesQueryResult>>
    {
        private readonly Inventory_Management_Context _context;
        public GetUnit_TypesQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task<List<GetUnit_TypesQueryResult>> Handle(GetUnit_TypesQuery request, CancellationToken cancellationToken)
        {
            var val = await _context.Unit_Types.ToListAsync();
            return val.Select(x => new GetUnit_TypesQueryResult
            {
                Id = x.Id,
                UnitName = x.UnitName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive

            }).ToList();
        }
    }
}