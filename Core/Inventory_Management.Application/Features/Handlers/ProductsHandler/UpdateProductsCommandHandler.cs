using Inventory_Management.Application.Features.Commands.ProductsCommand;
using Inventory_Management.Domain.Common;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.ProductsHandler
{
    public class UpdateProductsCommandHandler : IRequestHandler<UpdateProductsCommand>
    {
        private readonly Inventory_Management_Context _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateProductsCommandHandler(Inventory_Management_Context context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task Handle(UpdateProductsCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.CompanyId.HasValue)
            {
                throw new InvalidOperationException("User is not associated with a company.");
            }

            var val = await _context.Products.FindAsync(request.Id);
            val.CompanyId = _currentUserService.CompanyId.Value;
            val.ProductName = request.ProductName;
            val.Description = request.Description;
            val.Barcode = request.Barcode;
            val.ImageURL = request.ImageURL;
            val.CategoryId = request.CategoryId;
            val.UnitTypeId = request.UnitTypeId;
            val.IsActive = request.IsActive;
            val.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}