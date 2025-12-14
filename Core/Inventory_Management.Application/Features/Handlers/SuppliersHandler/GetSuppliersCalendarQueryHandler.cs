using Inventory_Management.Application.Features.Queries.SuppliersQuery;
using Inventory_Management.Application.Features.Results.SuppliersResult;
using Inventory_Management.Domain.Entities;
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
            var rules = await _context.Delivery_Rules
        .AsNoTracking()
        .Include(x => x.Supplier)
        .ToListAsync(cancellationToken);

            var result = new List<GetSuppliersCalenderQueryResult>();

            foreach (var rule in rules)
            {
                int interval = rule.Interval > 0 ? rule.Interval : 1;

                // ----------------------------------------------------------------
                // 1. HAFTALIK PLANLAR (Frequency = 1)
                // ----------------------------------------------------------------
                if (rule.Frequency == Delivery_Rules.FrequencyType.Weekly)
                {
                    if (string.IsNullOrWhiteSpace(rule.DaysOfWeek)) continue;

                    var targetDays = rule.DaysOfWeek.Split(',')
                        .Select(s => int.TryParse(s.Trim(), out int n) ? n : -1)
                        .Where(n => n >= 0 && n <= 6)
                        .ToArray();

                    if (targetDays.Length == 0) continue;

                    // SENARYO A: Her Hafta (Interval = 1) -> FullCalendar Recurring (Performanslı)
                    if (interval == 1)
                    {
                        result.Add(new GetSuppliersCalenderQueryResult
                        {
                            Id = rule.Id,
                            Title = rule.RuleName,
                            CalendarColor = rule.CalendarColor ?? "#3788d8",
                            // Her hafta olduğu için Array gönderiyoruz, FullCalendar bunu sever
                            DaysOfWeek = targetDays,
                            StartTime = rule.ArrivalTime.ToString(@"hh\:mm"),
                            StartRecur = rule.StartDate.ToString("yyyy-MM-dd"),
                            EndRecur = rule.EndDate?.AddDays(1).ToString("yyyy-MM-dd"),

                            Frequency = 1,
                            Interval = 1,
                            LeadTime = rule.LeadTimeDays,
                            Description = rule.RuleName,
                            SupplierId = rule.SupplierId,

                            // EKSİK ALANLAR EKLENDİ: Edit modalının doğru çalışması için gereklidir.
                            StartDate = rule.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            EndDate = rule.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                            ArrivalTime = rule.ArrivalTime.ToString(@"hh\:mm\:ss")
                        });
                    }
                                        // SENARYO B: Aralıklı Haftalar (Interval > 1) -> Tekil Eventler
                                        // "2 Haftada Bir" gibi durumlar
                                        else
                                        {
                                            var startDate = rule.StartDate.Date;
                                            var endDate = rule.EndDate ?? DateTime.Today.AddYears(1);
                    
                                            // HATA DÜZELTME: Döngü, kuralın başlangıç haftasına değil,
                                            // ilk gerçek olayın gerçekleştiği haftaya sabitlenmelidir.
                    
                                            // 1. İlk geçerli olay tarihini bul
                                            DateTime? firstEventDate = null;
                                            var tempDate = startDate;
                    
                                            // Sonsuz döngüden kaçınmak için makul bir sınır
                                            while (tempDate < startDate.AddYears(5)) 
                                            {
                                                if (targetDays.Contains((int)tempDate.DayOfWeek))
                                                {
                                                    firstEventDate = tempDate;
                                                    break;
                                                }
                                                tempDate = tempDate.AddDays(1);
                                            }
                    
                                            // Hiçbir zaman geçerli bir olay günü bulunamazsa bu kuralı atla
                                            if (firstEventDate == null) continue;
                    
                                            // 2. Döngüyü ilk olayın haftasından başlat
                                            var currentWeekStart = firstEventDate.Value.AddDays(-(int)firstEventDate.Value.DayOfWeek);
                    
                                            while (currentWeekStart <= endDate)
                                            {
                                                foreach (var dayCode in targetDays)
                                                {
                                                    var eventDate = currentWeekStart.AddDays(dayCode);
                    
                                                    if (eventDate >= startDate && eventDate <= endDate)
                                                    {
                                                        var fullDateTime = eventDate.Add(rule.ArrivalTime);
                    
                                                          result.Add(new GetSuppliersCalenderQueryResult
                                                          {
                                                            Id = rule.Id,
                                                            Title = rule.RuleName,
                                                            CalendarColor = rule.CalendarColor ?? "#3788d8",
                                                        
                                                            // ✅ KESİN ÇÖZÜM: FullCalendar tekrar etmesin diye Array'i NULL yapıyoruz
                                                            DaysOfWeek = null,                    
                                                            // ✅ HİLE: Edit modalı için günleri buraya string olarak saklıyoruz
                                                            // Frontend'de "DaysOfMonth" alanına bakıp checkboxları yakacağız
                                                            DaysOfMonth = rule.DaysOfWeek, 
                    
                                                            Start = fullDateTime.ToString("yyyy-MM-ddTHH:mm:ss"), // Kesin Tarih
                                                            Frequency = 1,
                                                            Interval = interval,
                                                            LeadTime = rule.LeadTimeDays,
                                                            Description = rule.RuleName,
                                                            SupplierId = rule.SupplierId,
                                                            StartDate = rule.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                                                            EndDate = rule.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                                                            ArrivalTime = rule.ArrivalTime.ToString(@"hh\:mm\:ss")
                                                        });
                                                    }
                                                }
                                                // Interval kadar hafta atla
                                                currentWeekStart = currentWeekStart.AddDays(7 * interval);
                                            }
                                        }                }
                // =================================================================
                // 2. AYLIK PLANLAR (Frequency = 2)
                // =================================================================
                else if (rule.Frequency == Delivery_Rules.FrequencyType.Monthly)
                {
                    if (string.IsNullOrWhiteSpace(rule.DaysOfMonth)) continue;

                    var daysOfMonth = rule.DaysOfMonth.Split(',')
                        .Select(s => int.TryParse(s.Trim(), out int n) ? n : -1)
                        .Where(n => n >= 1 && n <= 31)
                        .ToList();

                    if (daysOfMonth.Count == 0) continue;

                    var startDate = rule.StartDate.Date;
                    var endDate = rule.EndDate?.Date ?? startDate.AddYears(1);

                    // Döngü Başlangıcı: StartDate'in olduğu ayın 1. günü
                    var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);

                    while (currentMonth <= endDate)
                    {
                        var lastDayOfMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);

                        foreach (var dayOfMonth in daysOfMonth)
                        {
                            // Ay sonu kontrolü (Örn: Şubat 30 -> 28)
                            var actualDay = Math.Min(dayOfMonth, lastDayOfMonth);

                            var eventDate = new DateTime(currentMonth.Year, currentMonth.Month, actualDay)
                                .Add(rule.ArrivalTime);

                            if (eventDate >= startDate && eventDate <= endDate)
                            {
                                result.Add(new GetSuppliersCalenderQueryResult
                                {
                                    Id = rule.Id,
                                    Title = rule.RuleName,
                                    RuleName = rule.RuleName,
                                    CalendarColor = rule.CalendarColor ?? "#3788d8",

                                    // ✅ KESİN TARİH
                                    Start = eventDate.ToString("yyyy-MM-ddTHH:mm:ss"),

                                    // Edit Formu İçin Orijinal Veriler
                                    Frequency = 2,
                                    Interval = interval,
                                    DaysOfMonth = rule.DaysOfMonth,

                                    StartTime = rule.ArrivalTime.ToString(@"hh\:mm"),
                                    ArrivalTime = rule.ArrivalTime.ToString(@"hh\:mm\:ss"),
                                    StartDate = rule.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                                    EndDate = rule.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                                    LeadTime = rule.LeadTimeDays,
                                    LeadTimeDays = rule.LeadTimeDays,
                                    Description = rule.RuleName,
                                    SupplierId = rule.SupplierId
                                });
                            }
                        }

                        // ✅ DÜZELTME BURADAYDI: Doğrudan Interval kadar ay atla
                        currentMonth = currentMonth.AddMonths(interval);
                    }
                }
            }

            return result;
        }
    }
}