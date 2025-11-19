using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.Suppliers_DeliveryCommand
{
    public class DeleteSuppliers_DeliveryCommand : IRequest
    {
        public DeleteSuppliers_DeliveryCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}