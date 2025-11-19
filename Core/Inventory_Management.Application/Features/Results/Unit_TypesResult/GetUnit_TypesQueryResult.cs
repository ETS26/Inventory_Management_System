
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Unit_TypesResult
{
    public class GetUnit_TypesQueryResult : BaseEntity
    {
        public string UnitName { get; set; }
    }
}
