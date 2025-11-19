
using Inventory_Management.Application.Features.Results.CompaniesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.CompaniesQuery
{
    public class GetCompaniesByIdQuery : IRequest<GetCompaniesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetCompaniesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
