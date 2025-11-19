using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyRelationships_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Stock_Movements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductsId",
                table: "Stock_Movements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsersRoles_RoleId",
                table: "UsersRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersRoles_UserId",
                table: "UsersRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Deliveries_RuleId",
                table: "Suppliers_Deliveries",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Deliveries_SupplierId",
                table: "Suppliers_Deliveries",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_CompanyId",
                table: "Stock_Movements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_InventoryId",
                table: "Stock_Movements",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_MoveTypeId",
                table: "Stock_Movements",
                column: "MoveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_ProductsId",
                table: "Stock_Movements",
                column: "ProductsId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_SupplierId",
                table: "Stock_Movements",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Movements_UserId",
                table: "Stock_Movements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitTypeId",
                table: "Products",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_CompanyId",
                table: "Inventories",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Companies_CompanyId",
                table: "Inventories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Products_ProductId",
                table: "Inventories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Unit_Types_UnitTypeId",
                table: "Products",
                column: "UnitTypeId",
                principalTable: "Unit_Types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Movements_Companies_CompanyId",
                table: "Stock_Movements",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Movements_Inventories_InventoryId",
                table: "Stock_Movements",
                column: "InventoryId",
                principalTable: "Inventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Movements_Move_Types_MoveTypeId",
                table: "Stock_Movements",
                column: "MoveTypeId",
                principalTable: "Move_Types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Movements_Products_ProductsId",
                table: "Stock_Movements",
                column: "ProductsId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Movements_Suppliers_SupplierId",
                table: "Stock_Movements",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Movements_Users_UserId",
                table: "Stock_Movements",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Deliveries_Delivery_Rules_RuleId",
                table: "Suppliers_Deliveries",
                column: "RuleId",
                principalTable: "Delivery_Rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Deliveries_Suppliers_SupplierId",
                table: "Suppliers_Deliveries",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersRoles_Roles_RoleId",
                table: "UsersRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsersRoles_Users_UserId",
                table: "UsersRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Companies_CompanyId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Products_ProductId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Unit_Types_UnitTypeId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Movements_Companies_CompanyId",
                table: "Stock_Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Movements_Inventories_InventoryId",
                table: "Stock_Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Movements_Move_Types_MoveTypeId",
                table: "Stock_Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Movements_Products_ProductsId",
                table: "Stock_Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Movements_Suppliers_SupplierId",
                table: "Stock_Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Movements_Users_UserId",
                table: "Stock_Movements");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Deliveries_Delivery_Rules_RuleId",
                table: "Suppliers_Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Deliveries_Suppliers_SupplierId",
                table: "Suppliers_Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersRoles_Roles_RoleId",
                table: "UsersRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UsersRoles_Users_UserId",
                table: "UsersRoles");

            migrationBuilder.DropIndex(
                name: "IX_UsersRoles_RoleId",
                table: "UsersRoles");

            migrationBuilder.DropIndex(
                name: "IX_UsersRoles_UserId",
                table: "UsersRoles");

            migrationBuilder.DropIndex(
                name: "IX_Users_CompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Deliveries_RuleId",
                table: "Suppliers_Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Deliveries_SupplierId",
                table: "Suppliers_Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Movements_CompanyId",
                table: "Stock_Movements");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Movements_InventoryId",
                table: "Stock_Movements");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Movements_MoveTypeId",
                table: "Stock_Movements");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Movements_ProductsId",
                table: "Stock_Movements");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Movements_SupplierId",
                table: "Stock_Movements");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Movements_UserId",
                table: "Stock_Movements");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitTypeId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_CompanyId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Stock_Movements");

            migrationBuilder.DropColumn(
                name: "ProductsId",
                table: "Stock_Movements");
        }
    }
}
