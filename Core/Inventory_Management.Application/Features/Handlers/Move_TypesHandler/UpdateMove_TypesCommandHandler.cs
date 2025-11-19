using Inventory_Management.Application.Features.Commands.Move_TypesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Move_TypesHandler
{
    public class UpdateMove_TypesCommandHandler : IRequestHandler<UpdateMove_TypesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateMove_TypesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateMove_TypesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Move_Types.FindAsync(request.Id);
            val.MoveType = request.MoveType;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}