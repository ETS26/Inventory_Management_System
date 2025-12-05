using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddMultitenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "UsersRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE ur SET ur.CompanyId = u.CompanyId FROM UsersRoles ur INNER JOIN Users u ON ur.UserId = u.Id");
            
            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "UsersRoles",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_UsersRoles_CompanyId",
                table: "UsersRoles",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsersRoles_Companies_CompanyId",
                table: "UsersRoles",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersRoles_Companies_CompanyId",
                table: "UsersRoles");

            migrationBuilder.DropIndex(
                name: "IX_UsersRoles_CompanyId",
                table: "UsersRoles");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "UsersRoles");
        }
    }
}
