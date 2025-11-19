using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Delivery_RulesCommand
{
    public class DeleteDelivery_RulesCommand : IRequest
    {
        public DeleteDelivery_RulesCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}