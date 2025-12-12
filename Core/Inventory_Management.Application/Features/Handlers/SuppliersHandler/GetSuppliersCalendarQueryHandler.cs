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

                // HAFTALIK PLANLAR (Frequency = 1)

                if (rule.Frequency == Delivery_Rules.FrequencyType.Weekly)

                {

                    if (string.IsNullOrWhiteSpace(rule.DaysOfWeek)) continue;



                    var days = rule.DaysOfWeek.Split(',')

                        .Select(s => int.TryParse(s.Trim(), out int n) ? n : -1)

                        .Where(n => n >= 0 && n <= 6)

                        .ToArray();

                    var Weekinterval = rule.Interval > 0 ? rule.Interval : 1;



                    if (days.Length > 0)

                    {

                        result.Add(new GetSuppliersCalenderQueryResult

                        {

                            Id = rule.Id,

                            Title = rule.RuleName,

                            RuleName = rule.RuleName,

                            Color = rule.CalendarColor ?? "#3788d8",

                            CalendarColor = rule.CalendarColor ?? "#3788d8",

                            DaysOfWeek = days,

                            StartTime = rule.ArrivalTime.ToString(@"hh\:mm"),

                            ArrivalTime = rule.ArrivalTime.ToString(@"hh\:mm\:ss"),

                            LeadTime = rule.LeadTimeDays,

                            LeadTimeDays = rule.LeadTimeDays,

                            Description = rule.RuleName,

                            SupplierId = rule.SupplierId,

                            Frequency = (int)rule.Frequency,

                            Interval = rule.Interval,

                            StartRecur = rule.StartDate.ToString("yyyy-MM-dd"),

                            StartDate = rule.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),

                            EndRecur = rule.EndDate?.AddDays(1).ToString("yyyy-MM-dd"),

                            EndDate = rule.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss")

                        });

                    }

                }

                // AYLIK PLANLAR (Frequency = 2) - TAM YENİ

                else if (rule.Frequency == Delivery_Rules.FrequencyType.Monthly)

                {

                    // ✅ KRİTİK: DayOfMonth alanını kontrol et

                    if (string.IsNullOrWhiteSpace(rule.DaysOfMonth)) continue;



                    // "4,7,31" string'ini parse et

                    var DaysOfMonth = rule.DaysOfMonth.Split(',')

                        .Select(s => int.TryParse(s.Trim(), out int n) ? n : -1)

                        .Where(n => n >= 1 && n <= 31)

                        .ToList();



                    if (DaysOfMonth.Count == 0) continue;



                    // Her ay için tarihleri hesapla

                    var startDate = rule.StartDate;

                    var endDate = rule.EndDate ?? startDate.AddYears(1);

                    var Monthinterval = rule.Interval > 0 ? rule.Interval : 1;



                    var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);



                    while (currentMonth <= endDate)

                    {

                        var lastDayOfMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);



                        foreach (var DayOfMonth in DaysOfMonth)

                        {

                            // Ayda olmayan günleri son güne ayarla

                            var actualDay = Math.Min(DayOfMonth, lastDayOfMonth);

                            var eventDate = new DateTime(currentMonth.Year, currentMonth.Month, actualDay)

                                .Add(rule.ArrivalTime);



                            // Tarih aralığı kontrolü

                            if (eventDate >= startDate && eventDate <= endDate)

                            {

                                result.Add(new GetSuppliersCalenderQueryResult

                                {

                                    Id = rule.Id,

                                    Title = rule.RuleName,

                                    RuleName = rule.RuleName,

                                    Color = rule.CalendarColor ?? "#3788d8",

                                    CalendarColor = rule.CalendarColor ?? "#3788d8",

                                    Start = eventDate.ToString("yyyy-MM-ddTHH:mm:ss"), // ✅ TEKİL TARİH

                                    StartDate = rule.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),

                                    EndDate = rule.EndDate?.ToString("yyyy-MM-ddTHH:mm:ss"),

                                    ArrivalTime = rule.ArrivalTime.ToString(@"hh\:mm\:ss"),

                                    LeadTime = rule.LeadTimeDays,

                                    LeadTimeDays = rule.LeadTimeDays,

                                    Description = rule.RuleName,

                                    SupplierId = rule.SupplierId,

                                    Frequency = (int)rule.Frequency,

                                    Interval = Monthinterval,

                                    DaysOfMonth = rule.DaysOfMonth // ✅ "4,7,31" string'i

                                });

                            }

                        }



                        // Interval kadar ay atla

                        currentMonth = currentMonth.AddMonths(Monthinterval);

                    }

                }

            }



            return result;

        }

    }

}