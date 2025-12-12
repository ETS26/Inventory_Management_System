using Inventory_Management.Application.Features.Commands.Delivery_RulesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Delivery_RulesHandler
{
    public class UpdateDelivery_RulesCommandHandler : IRequestHandler<UpdateDelivery_RulesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateDelivery_RulesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateDelivery_RulesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Delivery_Rules.FindAsync(request.Id);
            if (val != null)
            {
                val.SupplierId = request.SupplierId;
                val.RuleName = request.RuleName;
                val.StartDate = request.StartDate;
                val.EndDate = request.EndDate;
                val.Frequency = request.Frequency;
                val.Interval = request.Interval;
                val.ArrivalTime = request.ArrivalTime;
                val.DaysOfWeek = request.DaysOfWeek;
                val.DaysOfMonth = request.DaysOfMonth;
                val.LeadTimeDays = request.LeadTimeDays;
                val.CalendarColor = request.CalendarColor;
                val.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}