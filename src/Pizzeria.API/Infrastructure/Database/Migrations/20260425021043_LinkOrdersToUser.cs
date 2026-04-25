using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pizzeria.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class LinkOrdersToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerUserId",
                table: "orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_orders_CustomerUserId",
                table: "orders",
                column: "CustomerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_users_CustomerUserId",
                table: "orders",
                column: "CustomerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_users_CustomerUserId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_CustomerUserId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "orders");
        }
    }
}
