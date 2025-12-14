using Inventory_Management.Application.Features.Commands.Delivery_RulesCommand;
using Inventory_Management.Domain.Common;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.Delivery_RulesHandler
{
    public class UpdateDelivery_RulesCommandHandler : IRequestHandler<UpdateDelivery_RulesCommand>
    {
        private readonly Inventory_Management_Context _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateDelivery_RulesCommandHandler(Inventory_Management_Context context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(UpdateDelivery_RulesCommand request, CancellationToken cancellationToken)
        {
            // 1. Get CompanyId from the current user's context
            var companyId = _currentUserService.CompanyId;
            if (companyId == null || companyId == Guid.Empty)
            {
                throw new Exception("User is not associated with a valid company.");
            }

            // 2. Validate other Foreign Keys like SupplierId
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == request.SupplierId && s.CompanyId == companyId, cancellationToken);
            if (!supplierExists)
            {
                throw new Exception($"Validation Error: Supplier with Id '{request.SupplierId}' does not exist for the current company.");
            }

            // 3. Fetch the entity to update
            var val = await _context.Delivery_Rules.FirstOrDefaultAsync(dr => dr.Id == request.Id && dr.CompanyId == companyId, cancellationToken);
            if (val != null)
            {
                // 4. Update properties
                val.CompanyId = companyId.Value; // Use the trusted CompanyId from the user's session
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

                // 5. Save changes
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new Exception($"Update Error: Delivery Rule with Id '{request.Id}' not found for the current company.");
            }
        }
    }
}