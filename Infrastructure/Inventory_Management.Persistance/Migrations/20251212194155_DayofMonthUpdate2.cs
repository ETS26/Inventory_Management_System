using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class DayofMonthUpdate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DayOfMonth",
                table: "Delivery_Rules",
                newName: "DaysOfMonth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DaysOfMonth",
                table: "Delivery_Rules",
                newName: "DayOfMonth");
        }
    }
}
