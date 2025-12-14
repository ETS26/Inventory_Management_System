using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.InventoriesCommand;

namespace Inventory_Management.Application.Features.Handlers.InventoriesHandler
{
    public class DeleteInventoriesCommandHandler : IRequestHandler<DeleteInventoriesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteInventoriesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteInventoriesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Inventories
                .FindAsync(new object[] { request.Id }, cancellationToken);

            // 2. Kontrol Et: Kayıt var mı?
            if (val == null)
            {
                // Eğer kayıt yoksa (veya filtreye takıldıysa) hata fırlat
                throw new Exception("Silinecek kayıt bulunamadı. (ID yanlış olabilir veya bu kayda erişim yetkiniz yok)");
            }

            // 3. İlişkili Veri Kontrolü (Opsiyonel ama Önerilir)
            // Eğer bu kural bir tedarikçiye atanmışsa, SQL tarafında Foreign Key hatası alabilirsiniz.
            // Onu engellemek için silmeden önce ilişkili tabloları kontrol etmek gerekebilir.
            // Şimdilik sadece silmeyi deniyoruz:

            _context.Inventories.Remove(val);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}