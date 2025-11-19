using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Move_TypesCommand
{
    public class UpdateMove_TypesCommand : IRequest
    {
        public Guid Id { get; set; }
        public string MoveType { get; set; }
        public bool IsActive { get; set; }
    }
}