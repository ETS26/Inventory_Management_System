
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Move_TypesResult
{
    public class GetMove_TypesByIdQueryResult : BaseEntity
    {
        public string MoveType { get; set; }
    }
}
