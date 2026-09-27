using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDeletionProcesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_deletion_processes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    requested_by_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_order_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shipping_date = table.Column<DateOnly>(type: "date", nullable: true),
                    item_count = table.Column<int>(type: "integer", nullable: false),
                    item_summary_json = table.Column<string>(type: "jsonb", nullable: false),
                    production_task_count = table.Column<int>(type: "integer", nullable: false),
                    shipment_count = table.Column<int>(type: "integer", nullable: false),
                    had_financial_settlement = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_deletion_processes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_deletion_processes_order_id",
                table: "order_deletion_processes",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_deletion_processes_requested_at",
                table: "order_deletion_processes",
                column: "requested_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_deletion_processes");
        }
    }
}
