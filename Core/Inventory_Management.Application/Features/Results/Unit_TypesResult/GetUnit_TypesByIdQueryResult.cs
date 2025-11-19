
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Unit_TypesResult
{
    public class GetUnit_TypesByIdQueryResult : BaseEntity
    {
        public string UnitName { get; set; }
    }
}
