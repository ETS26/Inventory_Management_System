using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Management.Application.Features.Results.SuppliersResult
{
    public class GetSuppliersCalenderQueryResult
    {
        public Guid Id { get; set; }           // YENİ: Silmek için ID lazım
        public string Title { get; set; }
        public string Color { get; set; }
        public int[] DaysOfWeek { get; set; }
        public string StartTime { get; set; }

        // --- YENİ EKLENENLER (Sınırlandırma İçin) ---
        public string StartRecur { get; set; } // Başlangıç Tarihi
        public string? EndRecur { get; set; }  // Bitiş Tarihi

        public int LeadTime { get; set; }
        public string Description { get; set; }
    }
}
