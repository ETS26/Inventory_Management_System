using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.ProductsCommand;

namespace Inventory_Management.Application.Features.Handlers.ProductsHandler
{
    public class DeleteProductsCommandHandler : IRequestHandler<DeleteProductsCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteProductsCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteProductsCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Products.FindAsync(request.Id);
            _context.Products.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}