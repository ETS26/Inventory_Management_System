using MediatR;
using Inventory_Management.Persistance.Context;
using System.Threading;
using System.Threading.Tasks;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Application.Features.Commands.Delivery_RulesCommand;

namespace Inventory_Management.Application.Features.Handlers.Delivery_RulesHandler
{
    public class DeleteDelivery_RulesCommandHandler : IRequestHandler<DeleteDelivery_RulesCommand>
    {
        private readonly Inventory_Management_Context _context;

        public DeleteDelivery_RulesCommandHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task Handle(DeleteDelivery_RulesCommand request, CancellationToken cancellationToken)
        {
            var val = await _context.Delivery_Rules
                .FindAsync(new object[] { request.Id }, cancellationToken);

            // 2. Kontrol Et: Kayýt var mý?
            if (val == null)
            {
                // Eðer kayýt yoksa (veya filtreye takýldýysa) hata fýrlat
                throw new Exception("Silinecek kayýt bulunamadý. (ID yanlýþ olabilir veya bu kayda eriþim yetkiniz yok)");
            }

            // 3. Ýliþkili Veri Kontrolü (Opsiyonel ama Önerilir)
            // Eðer bu kural bir tedarikçiye atanmýþsa, SQL tarafýnda Foreign Key hatasý alabilirsiniz.
            // Onu engellemek için silmeden önce iliþkili tablolarý kontrol etmek gerekebilir.
            // Þimdilik sadece silmeyi deniyoruz:

            _context.Delivery_Rules.Remove(val);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}