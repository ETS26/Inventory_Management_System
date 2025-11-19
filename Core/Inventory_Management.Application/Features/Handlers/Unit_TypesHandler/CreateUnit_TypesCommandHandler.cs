using Inventory_Management.Application.Features.Commands.Unit_TypesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Unit_TypesHandler
{
    public class CreateUnit_TypesCommandHandler : IRequestHandler<CreateUnit_TypesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateUnit_TypesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateUnit_TypesCommand request, CancellationToken cancellationToken)
        {
            await _context.Unit_Types.AddAsync(new Unit_Types
            {
                UnitName = request.UnitName,
            });
            await _context.SaveChangesAsync();
            
        }
    }
}