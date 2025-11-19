namespace Inventory_Management.Domain.Entities
{
    public class Delivery_Rules : BaseEntity
    {
        public string RuleName { get; set; }
        public string? RuleDescription { get; set; }
    }
}