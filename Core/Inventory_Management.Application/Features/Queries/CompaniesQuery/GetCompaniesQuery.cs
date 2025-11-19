
using Inventory_Management.Application.Features.Results.CompaniesResult;
using MediatR;
using System.Collections.Generic;

namespace Inventory_Management.Application.Features.Queries.CompaniesQuery
{
    public class GetCompaniesQuery : IRequest<List<GetCompaniesQueryResult>>
    {
    }
}
