using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Unit_TypesCommand
{
    public class CreateUnit_TypesCommand : IRequest
    {
        public string UnitName { get; set; }
    }
}