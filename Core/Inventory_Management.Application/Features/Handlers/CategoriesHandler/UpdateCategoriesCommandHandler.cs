
using Inventory_Management.Application.Features.Commands.CategoriesCommand;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CategoriesHandler
{
    public class UpdateCategoriesCommandHandler : IRequestHandler<UpdateCategoriesCommand>
    {
        private readonly Inventory_Management_Context _context;
        public UpdateCategoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }
        public async Task Handle(UpdateCategoriesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Categories.FindAsync(request.Id);
            val.CompanyId = request.CompanyId;
            val.CategoryName = request.CategoryName;
            val.Description = request.Description;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
