using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryRulesUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Suppliers_Deliveries");

            migrationBuilder.DropColumn(
                name: "IsFriday",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "IsMonday",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "IsSaturday",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "IsSunday",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "IsThursday",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "IsTuesday",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "IsWednesday",
                table: "Delivery_Rules");

            migrationBuilder.RenameColumn(
                name: "RuleDescription",
                table: "Delivery_Rules",
                newName: "DaysOfWeek");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ArrivalTime",
                table: "Delivery_Rules",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Delivery_Rules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DayOfMonth",
                table: "Delivery_Rules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Delivery_Rules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "Delivery_Rules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "Delivery_Rules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Delivery_Rules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "Delivery_Rules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_Rules_CompanyId",
                table: "Delivery_Rules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_Rules_SupplierId",
                table: "Delivery_Rules",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Rules_Companies_CompanyId",
                table: "Delivery_Rules",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Rules_Suppliers_SupplierId",
                table: "Delivery_Rules",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Rules_Companies_CompanyId",
                table: "Delivery_Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Rules_Suppliers_SupplierId",
                table: "Delivery_Rules");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_Rules_CompanyId",
                table: "Delivery_Rules");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_Rules_SupplierId",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "DayOfMonth",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Delivery_Rules");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Delivery_Rules");

            migrationBuilder.RenameColumn(
                name: "DaysOfWeek",
                table: "Delivery_Rules",
                newName: "RuleDescription");

            migrationBuilder.AddColumn<bool>(
                name: "IsFriday",
                table: "Delivery_Rules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMonday",
                table: "Delivery_Rules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSaturday",
                table: "Delivery_Rules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSunday",
                table: "Delivery_Rules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsThursday",
                table: "Delivery_Rules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTuesday",
                table: "Delivery_Rules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWednesday",
                table: "Delivery_Rules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Suppliers_Deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers_Deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suppliers_Deliveries_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Suppliers_Deliveries_Delivery_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Delivery_Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Suppliers_Deliveries_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Deliveries_CompanyId",
                table: "Suppliers_Deliveries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Deliveries_RuleId",
                table: "Suppliers_Deliveries",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Deliveries_SupplierId",
                table: "Suppliers_Deliveries",
                column: "SupplierId");
        }
    }
}
