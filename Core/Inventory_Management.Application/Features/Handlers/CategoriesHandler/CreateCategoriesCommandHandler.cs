
using Inventory_Management.Application.Features.Commands.CategoriesCommand;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.CategoriesHandler
{
    public class CreateCategoriesCommandHandler : IRequestHandler<CreateCategoriesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public CreateCategoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(CreateCategoriesCommand request, CancellationToken cancellationToken)
        {
            await _context.Categories.AddAsync(new Categories
            {
                CompanyId= request.CompanyId,
                CategoryName = request.CategoryName,
                Description= request.Description
            });
            await _context.SaveChangesAsync();
            
        }
    }
}
