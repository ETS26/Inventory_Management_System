using Inventory_Management.Application.Features.Commands.ProductsCommand;
using Inventory_Management.Domain.Common;
using Inventory_Management.Application.Features.Exceptions;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Inventory_Management.Application.Features.Handlers.ProductsHandler
{
    public class ActivateProductCommandHandler : IRequestHandler<ActivateProductCommand>
    {
        private readonly Inventory_Management_Context _context;
        private readonly ICurrentUserService _currentUserService;

        public ActivateProductCommandHandler(Inventory_Management_Context context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task Handle(ActivateProductCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUserService.CompanyId;
            if (companyId == null)
            {
                throw new BadRequestException("User is not associated with a company.");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.CompanyId == companyId, cancellationToken);

            if (product == null)
            {
                throw new NotFoundException("Product not found.");
            }

            product.IsActive = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
