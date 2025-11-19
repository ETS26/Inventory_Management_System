
using Inventory_Management.Application.Features.Results.CategoriesResult;
using MediatR;
using System;

namespace Inventory_Management.Application.Features.Queries.CategoriesQuery
{
    public class GetCategoriesByIdQuery : IRequest<GetCategoriesByIdQueryResult>
    {
        public Guid Id { get; set; }

        public GetCategoriesByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
