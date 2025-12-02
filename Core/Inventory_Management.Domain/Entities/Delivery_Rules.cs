using System;

namespace Inventory_Management.Domain.Entities
{
    public class Delivery_Rules : BaseEntity
    {
        public string RuleName { get; set; } // Örn: "Standart Kargo", "Hafta Sonu Teslimat"
        public string? RuleDescription { get; set; }

        // --- TAKVÝM ÝÇÝN GEREKLÝ ALANLAR ---

        // 1. Teslimat Süresi (Lead Time): Sipariþten kaç gün sonra gelir?
        // Örn: 1 (Ertesi gün), 0 (Ayný gün), 3 (3 gün sonra)
        public int LeadTimeDays { get; set; }

        // 2. Takvim Rengi (Hex Code): Frontend'de ayrým yapmak için
        // Örn: "#0d6efd" (Mavi), "#dc3545" (Kýrmýzý)
        public string CalendarColor { get; set; }

        // 3. Hangi Günler Teslimat Var? (Recurring Pattern)
        public bool IsMonday { get; set; }
        public bool IsTuesday { get; set; }
        public bool IsWednesday { get; set; }
        public bool IsThursday { get; set; }
        public bool IsFriday { get; set; }
        public bool IsSaturday { get; set; }
        public bool IsSunday { get; set; }

        // Ýliþkiler
        // public virtual ICollection<Suppliers_Delivery> Suppliers_Deliveries { get; set; }
    }
}