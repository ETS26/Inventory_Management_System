using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Stock_MovementsCommand
{
    public class CreateStock_MovementsCommand : IRequest
    {
        public Guid InventoryId { get; set; }
        public Guid MoveTypeId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }
    }
}