public class GetSuppliersCalenderQueryResult
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string RuleName { get; set; }
    public string Color { get; set; }
    public string CalendarColor { get; set; }

    // Haftalık için
    public int[]? DaysOfWeek { get; set; }
    public string? StartTime { get; set; }
    public string? StartRecur { get; set; }
    public string? EndRecur { get; set; }

    // Aylık için
    public string? Start { get; set; } // Tekil tarih "2025-05-15T14:30:00"
    public string? DaysOfMonth { get; set; } // "4,7,31" string

    // Ortak
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? ArrivalTime { get; set; }
    public int LeadTime { get; set; }
    public int LeadTimeDays { get; set; }
    public string? Description { get; set; }
    public Guid SupplierId { get; set; }
    public int Frequency { get; set; }
    public int Interval { get; set; }
}