using Inventory_Management.Domain.Common;
using System;

namespace Inventory_Management.Domain.Entities
{
    public class Delivery_Rules : BaseEntity,IHasCompany
    {
        public enum FrequencyType
        {
            Weekly = 1, // Haftalýk
            Monthly = 2 // Aylýk
        }
        public Guid CompanyId { get; set; }
        public virtual Companies Company { get; set; }
        public Guid SupplierId { get; set; }
        public virtual Suppliers Supplier { get; set; }
        public string RuleName { get; set; } // Örn: "Yaz Sezonu Süt Sevkiyatý"

        // --- PLANLAMA AYARLARI ---

        public DateTime StartDate { get; set; } // Plan Baþlangýç
        public DateTime? EndDate { get; set; }   // Plan Bitiþ (Boþsa sonsuza kadar)

        public FrequencyType Frequency { get; set; } // Haftalýk mý, Aylýk mý?
        public int Interval { get; set; } = 1;       // Kaç haftada/ayda bir? (Varsayýlan 1)
        public TimeSpan ArrivalTime { get; set; }

        // Haftalýk ise hangi günler? (Pzt, Sal...)
        // Bunlarý tek bir string'de tutabiliriz: "1,3,5" gibi (Pzt, Çar, Cum)
        public string? DaysOfWeek { get; set; }

        // Aylýk ise ayýn kaçýncý günü? (Örn: 15'i)
        public int? DayOfMonth { get; set; }

        public int LeadTimeDays { get; set; } // Teslimat süresi
        public string CalendarColor { get; set; }
    }
}