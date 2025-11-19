using Inventory_Management.Application.Features.Commands.Unit_TypesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Unit_TypesHandler
{
    public class UpdateUnit_TypesCommandHandler : IRequestHandler<UpdateUnit_TypesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateUnit_TypesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateUnit_TypesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Unit_Types.FindAsync(request.Id);
            val.UnitName = request.UnitName;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}