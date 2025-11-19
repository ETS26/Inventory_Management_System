using Inventory_Management.Application.Features.Commands.Move_TypesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Move_TypesHandler
{
    public class CreateMove_TypesCommandHandler : IRequestHandler<CreateMove_TypesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateMove_TypesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateMove_TypesCommand request, CancellationToken cancellationToken)
        {
            await _context.Move_Types.AddAsync(new Move_Types
            {
                MoveType = request.MoveType
            });
            await _context.SaveChangesAsync();
            
        }
    }
}