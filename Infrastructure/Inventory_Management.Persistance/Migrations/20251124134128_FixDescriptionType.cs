using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class FixDescriptionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Movements_Products_ProductsId",
                table: "Stock_Movements");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Movements_ProductsId",
                table: "Stock_Movements");

            migrationBuilder.DropColumn(
                name: "ProductsId",
                table: "Stock_Movements");

            migrationBuilder.AlterColumn<float>(
                name: "Description",
                table: "Stock_Movements",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RuleDescription",
                table: "Delivery_Rules",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Stock_Movements",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductsId",
                table: "Stock_Movements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RuleDescription",
                table: "Delivery_Rules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_ProductsId",
                table: "Stock_Movements",
                column: "ProductsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Movements_Products_ProductsId",
                table: "Stock_Movements",
                column: "ProductsId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
