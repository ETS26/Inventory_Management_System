namespace Inventory_Management.Domain.Entities
{
    public class Move_Types : BaseEntity
    {
        public string MoveType { get; set; }

        public virtual ICollection<Stock_Movements> Stock_Movements { get; set; }
    }
}