using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Unit_TypesCommand
{
    public class UpdateUnit_TypesCommand : IRequest
    {
        public Guid Id { get; set; }
        public string UnitName { get; set; }
        public bool IsActive { get; set; }
    }
}