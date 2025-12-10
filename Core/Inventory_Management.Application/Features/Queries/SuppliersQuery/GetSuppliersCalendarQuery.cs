using Inventory_Management.Application.Features.Results.SuppliersResult;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Queries.SuppliersQuery
{
    public class GetSuppliersCalendarQuery : IRequest<List<GetSuppliersCalenderQueryResult>>
    {
    }
}
