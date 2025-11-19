using Inventory_Management.Application.Features.Commands.Delivery_RulesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Delivery_RulesHandler
{
    public class CreateDelivery_RulesCommandHandler : IRequestHandler<CreateDelivery_RulesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateDelivery_RulesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateDelivery_RulesCommand request, CancellationToken cancellationToken)
        {
            await _context.Delivery_Rules.AddAsync(new Delivery_Rules
            {
                RuleName = request.RuleName,
                RuleDescription = request.RuleDescription
            });
            await _context.SaveChangesAsync();
            
        }
    }
}