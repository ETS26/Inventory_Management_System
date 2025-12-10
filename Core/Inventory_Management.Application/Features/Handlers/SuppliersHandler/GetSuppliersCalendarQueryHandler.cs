using Inventory_Management.Application.Features.Queries.SuppliersQuery;
using Inventory_Management.Application.Features.Results.SuppliersResult;
using Inventory_Management.Persistance.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Handlers.SuppliersHandler
{
    public class GetSuppliersCalendarQueryHandler : IRequestHandler<GetSuppliersCalendarQuery, List<GetSuppliersCalenderQueryResult>>
    {
        private readonly Inventory_Management_Context _context;

        public GetSuppliersCalendarQueryHandler(Inventory_Management_Context context)
        {
            _context = context;
        }

        public async Task<List<GetSuppliersCalenderQueryResult>> Handle(GetSuppliersCalendarQuery request, CancellationToken cancellationToken)
        {
            // 1. Kuralları ve Bağlı Olduğu Tedarikçiyi Çek
            var rules = await _context.Delivery_Rules
                .AsNoTracking()
                .Include(x => x.Company) // Gerekirse
                                         // Eğer Delivery_Rules tablosunda SupplierId varsa:
                                         // .Include(x => x.Supplier) 
                                         // NOT: Eğer Delivery_Rules tablosunda Supplier ilişkisi yoksa,
                                         // bunu Suppliers_Delivery tablosundan joinleyerek çekmemiz gerekebilir.
                                         // Ancak son yapımızda Delivery_Rules içine SupplierId eklememişsek,
                                         // Suppliers_Delivery tablosunu sorgulamak daha doğru olabilir.

                // HIZLI ÇÖZÜM İÇİN VARSAYIM: 
                // Delivery_Rules tablosunu doğrudan sorguluyoruz.
                .ToListAsync(cancellationToken);

            // Eğer Tedarikçi İsmi Delivery_Rules'da yoksa, Suppliers_Delivery'den çekmeliyiz.
            // Ama basitlik adına şimdilik kural ismini başlık yapıyoruz.

            var result = new List<GetSuppliersCalenderQueryResult>();

            foreach (var rule in rules)
            {
                // Günler boşsa atla
                if (string.IsNullOrWhiteSpace(rule.DaysOfWeek)) continue;

                // "1,3,5" stringini [1, 3, 5] dizisine çevir
                var days = rule.DaysOfWeek.Split(',')
                                        .Select(s => int.TryParse(s, out int n) ? n : -1)
                                        .Where(n => n != -1)
                                        .ToArray();

                if (days.Length > 0)
                {
                    result.Add(new GetSuppliersCalenderQueryResult
                    {
                        Id = rule.Id,

                        // Başlık: Kural Adı (veya Tedarikçi Adı eklenebilir)
                        Title = rule.RuleName,

                        Color = rule.CalendarColor ?? "#3788d8",
                        DaysOfWeek = days,

                        // TimeSpan'i String saate çevir (14:30:00 -> "14:30")
                        StartTime = rule.ArrivalTime.ToString(@"hh\:mm"),

                        LeadTime = rule.LeadTimeDays,
                        Description = rule.RuleName, // veya Description alanı

                        // --- TARİH SINIRLAMASI ---
                        StartRecur = rule.StartDate.ToString("yyyy-MM-dd"),

                        // Bitiş tarihi varsa +1 gün ekle (FullCalendar kuralı), yoksa null bırak
                        EndRecur = rule.EndDate.HasValue
                            ? rule.EndDate.Value.AddDays(1).ToString("yyyy-MM-dd")
                            : null
                    });
                }
            }

            return result;
        }
    }
}