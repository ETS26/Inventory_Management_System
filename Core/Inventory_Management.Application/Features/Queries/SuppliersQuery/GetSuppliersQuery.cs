
using Inventory_Management.Application.Features.Results.SuppliersResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.SuppliersQuery
{
    public class GetSuppliersQuery : IRequest<List<GetSuppliersQueryResult>>
    {
    }
}
