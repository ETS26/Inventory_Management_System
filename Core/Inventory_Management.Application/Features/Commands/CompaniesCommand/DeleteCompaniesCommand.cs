using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.CompaniesCommand
{
    public class DeleteCompaniesCommand : IRequest
    {
        public DeleteCompaniesCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}