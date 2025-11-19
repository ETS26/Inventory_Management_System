
using Inventory_Management.Domain.Entities;
using System;

namespace Inventory_Management.Application.Features.Results.Stock_MovementsResult
{
    public class GetStock_MovementsQueryResult : BaseEntity
    {
        public Guid InventoryId { get; set; }
        public Guid MoveTypeId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public float Payment { get; set; }
        public float Description { get; set; }
    }
}
