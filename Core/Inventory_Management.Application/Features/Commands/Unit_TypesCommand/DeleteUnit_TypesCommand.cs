using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Unit_TypesCommand
{
    public class DeleteUnit_TypesCommand : IRequest
    {
        public DeleteUnit_TypesCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}