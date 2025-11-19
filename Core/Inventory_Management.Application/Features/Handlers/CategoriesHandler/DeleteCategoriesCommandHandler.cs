
using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.CategoriesCommand;

namespace Inventory_Management.Application.Features.Handlers.CategoriesHandler
{
    public class DeleteCategoriesCommandHandler : IRequestHandler<DeleteCategoriesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteCategoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteCategoriesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Categories.FindAsync(request.Id);
            _context.Categories.Remove(val);
            await _context.SaveChangesAsync();
        }
    }
}
