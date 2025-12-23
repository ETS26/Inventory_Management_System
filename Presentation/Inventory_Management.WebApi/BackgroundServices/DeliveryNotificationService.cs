using Inventory_Management.Application.Interfaces;
using Inventory_Management.Domain.Entities;
using Inventory_Management.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory_Management.WebApi.BackgroundServices
{
    public class DeliveryNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeliveryNotificationService> _logger;

        // Servisin çalışma sıklığı (Örn: Her 30 dakikada bir kontrol et)
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

        public DeliveryNotificationService(IServiceScopeFactory scopeFactory, ILogger<DeliveryNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Delivery Notification Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotifications();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking delivery notifications.");
                }

                // Belirlenen aralık kadar bekle
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndSendNotifications()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Inventory_Management_Context>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // Tüm aktif teslimat kurallarını çek (Şirket ve Kullanıcılar dahil)
                var activeRules = await context.Delivery_Rules
                    .Include(r => r.Supplier)
                    .Include(r => r.Company)
                        .ThenInclude(c => c.Users)
                    .Where(r => r.IsActive)
                    .ToListAsync();

                // TÜRKİYE SAATİ AYARI (UTC+3)
                var now = DateTime.UtcNow.AddHours(3); 

                foreach (var rule in activeRules)
                {
                    DateTime? nextDelivery = GetNextOccurrence(rule, now);

                    if (nextDelivery.HasValue)
                    {
                        DateTime notificationTime = nextDelivery.Value.AddDays(-rule.LeadTimeDays);
                        TimeSpan diff = now - notificationTime;

                        if (diff.TotalMinutes >= 0 && diff.TotalMinutes < _checkInterval.TotalMinutes)
                        {
                            await SendNotificationEmail(emailService, rule, nextDelivery.Value);
                        }
                    }
                }
            }
        }

        private async Task SendNotificationEmail(IEmailService emailService, Delivery_Rules rule, DateTime deliveryDate)
        {
            if (rule.Company == null || rule.Company.Users == null || !rule.Company.Users.Any())
            {
                _logger.LogWarning($"No users found for company '{rule.Company?.CompanyName}' in rule '{rule.RuleName}'. Notification skipped.");
                return;
            }

            string subject = $"📦 Teslimat Hatırlatması: {rule.RuleName}";
            string body = $@"
                <h3>Teslimat Hatırlatması</h3>
                <p><strong>{rule.Company.CompanyName}</strong> çalışanlarının dikkatine,</p>
                <p>Aşağıdaki plana ait teslimatın yaklaşmakta olduğu tespit edilmiştir.</p>
                <ul>
                    <li><strong>Tedarikçi:</strong> {rule.Supplier?.SupplierName ?? "Bilinmiyor"}</li>
                    <li><strong>Plan Adı:</strong> {rule.RuleName}</li>
                    <li><strong>Teslimat Tarihi:</strong> {deliveryDate:dd.MM.yyyy}</li>
                    <li><strong>Teslimat Saati:</strong> {deliveryDate:HH:mm}</li>
                    <li><strong>Kalan Süre (Lead Time):</strong> {rule.LeadTimeDays} Gün</li>
                </ul>
                <p>Lütfen gerekli hazırlıkları yapınız.</p>
                <hr>
                <small>Inventory Management System AI Assistant</small>
            ";

            foreach (var user in rule.Company.Users)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogInformation($"Sending notification for rule '{rule.RuleName}' to {user.Email} (Company: {rule.Company.CompanyName})");
                    await emailService.SendEmailAsync(user.Email, subject, body);
                }
            }
        }

        /// <summary>
        /// Verilen kurala göre bugünden sonraki İLK uygun teslimat tarihini hesaplar.
        /// </summary>
        private DateTime? GetNextOccurrence(Delivery_Rules rule, DateTime referenceDate)
        {
            // Basitlik adına, referans tarihinden itibaren 1 yıl ileriye kadar kontrol edelim.
            // Çok karmaşık recurrence (tekrar) kuralları için özel kütüphaneler kullanılabilir.
            
            DateTime checkDate = rule.StartDate.Date > referenceDate.Date ? rule.StartDate.Date : referenceDate.Date;
            // Başlangıç saati ayarla
            checkDate = checkDate.Date + rule.ArrivalTime;

            // Eğer hesaplanan başlangıç (bugün+saat) geçmişte kaldıysa yarına/geleceğe bakmalıyız
            if (checkDate < referenceDate) 
            {
                 checkDate = checkDate.AddDays(1);
            }

            for (int i = 0; i < 365; i++)
            {
                // Tarih aralığı kontrolü
                if (rule.EndDate.HasValue && checkDate > rule.EndDate.Value)
                    return null;

                bool isMatch = false;

                if (rule.Frequency == Delivery_Rules.FrequencyType.Weekly)
                {
                    // Haftalık Kontrol
                    // 1. Gün uygun mu?
                    if (!string.IsNullOrEmpty(rule.DaysOfWeek))
                    {
                        var allowedDays = rule.DaysOfWeek.Split(',').Select(d => int.Parse(d.Trim())).ToList();
                        // DayOfWeek: Sunday=0, Monday=1...
                        // DB'de genelde 1=Pzt, 7=Pazar tutuluyorsa dönüşüm gerekebilir.
                        // Standart DayOfWeek enum ile uyumlu varsayıyoruz (0-6). 
                        // Veya UI 1-7 gönderiyorsa (1=Pzt) : 
                        int currentDayInt = (int)checkDate.DayOfWeek; 
                        if (currentDayInt == 0) currentDayInt = 7; // Pazar'ı 7 yapalım (ISO8601)

                        if (allowedDays.Contains(currentDayInt))
                        {
                            // 2. Interval (Hafta atlama) kontrolü
                            // Başlangıç tarihinden bu yana kaç hafta geçti?
                            TimeSpan diff = checkDate.Date - rule.StartDate.Date;
                            int weeksPassed = diff.Days / 7;
                            
                            if (weeksPassed % rule.Interval == 0)
                            {
                                isMatch = true;
                            }
                        }
                    }
                }
                else if (rule.Frequency == Delivery_Rules.FrequencyType.Monthly)
                {
                    // Aylık Kontrol
                    if (!string.IsNullOrEmpty(rule.DaysOfMonth))
                    {
                        var allowedDays = rule.DaysOfMonth.Split(',').Select(d => int.Parse(d.Trim())).ToList();
                        
                        if (allowedDays.Contains(checkDate.Day))
                        {
                             // Interval (Ay atlama) kontrolü
                             // (Yıl farkı * 12) + Ay farkı
                             int monthsPassed = ((checkDate.Year - rule.StartDate.Year) * 12) + checkDate.Month - rule.StartDate.Month;
                             
                             if (monthsPassed % rule.Interval == 0)
                             {
                                 isMatch = true;
                             }
                        }
                    }
                }

                if (isMatch)
                {
                    return checkDate;
                }

                // Bir sonraki güne geç
                checkDate = checkDate.AddDays(1);
            }

            return null;
        }
    }
}
