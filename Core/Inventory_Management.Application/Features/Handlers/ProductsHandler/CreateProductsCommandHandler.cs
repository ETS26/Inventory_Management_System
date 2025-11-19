using Inventory_Management.Application.Features.Commands.ProductsCommand;
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

        public CreateProductsCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateProductsCommand request, CancellationToken cancellationToken)
        {
            await _context.Products.AddAsync(new Products
            {
                ProductName = request.ProductName,
                Description = request.Description,
                Barcode = request.Barcode,
                CategoryId = request.CategoryId,
                UnitTypeId = request.UnitTypeId
            });
            await _context.SaveChangesAsync();
            
        }
    }
}