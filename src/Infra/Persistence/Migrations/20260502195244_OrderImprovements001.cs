using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderImprovements001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_email",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "orders");

            migrationBuilder.AddColumn<DateOnly>(
                name: "shipping_date",
                table: "orders",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_sources", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_sources_name",
                table: "order_sources",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_sources");

            migrationBuilder.DropColumn(
                name: "shipping_date",
                table: "orders");

            migrationBuilder.AddColumn<string>(
                name: "customer_email",
                table: "orders",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
