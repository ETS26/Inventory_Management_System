using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Move_TypesCommand
{
    public class CreateMove_TypesCommand : IRequest
    {
        public string MoveType { get; set; }
    }
}