using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "order_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_order_id",
                table: "stock_movements",
                column: "order_id");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_orders_order_id",
                table: "stock_movements",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_orders_order_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_order_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "stock_movements");
        }
    }
}
