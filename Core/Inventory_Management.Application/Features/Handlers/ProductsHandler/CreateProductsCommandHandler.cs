using Inventory_Management.Application.Features.Commands.ProductsCommand;
using Inventory_Management.Domain.Common;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.ProductsHandler
{
    public class CreateProductsCommandHandler : IRequestHandler<CreateProductsCommand>
    {
        private readonly Inventory_Management_Context _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateProductsCommandHandler(Inventory_Management_Context context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(CreateProductsCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.CompanyId.HasValue)
            {
                throw new InvalidOperationException("User is not associated with a company.");
            }
            await _context.Products.AddAsync(new Products
            {
                CompanyId = _currentUserService.CompanyId.Value,
                ProductName = request.ProductName,
                Description = request.Description,
                ImageURL = request.ImageURL,
                Barcode = request.Barcode,
                CategoryId = request.CategoryId,
                UnitTypeId = request.UnitTypeId
            });
            await _context.SaveChangesAsync();

        }
    }
}