using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.Delivery_RulesCommand;

namespace Inventory_Management.Application.Features.Handlers.Delivery_RulesHandler
{
    public class DeleteDelivery_RulesCommandHandler : IRequestHandler<DeleteDelivery_RulesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteDelivery_RulesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteDelivery_RulesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Delivery_Rules.FindAsync(request.Id);
            _context.Delivery_Rules.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}