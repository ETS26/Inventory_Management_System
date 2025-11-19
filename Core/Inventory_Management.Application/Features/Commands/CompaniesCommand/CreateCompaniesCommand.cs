using MediatR;
using System;

namespace Inventory_Management.Application.Features.Commands.CompaniesCommand
{
    public class CreateCompaniesCommand : IRequest
    {
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }
}