using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

public partial class AddInventoryAndProduction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "raw_materials",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_raw_materials", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "production_tasks",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                planned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_production_tasks", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "stock_movements",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                raw_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                business_reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stock_movements", x => x.id);
                table.ForeignKey(
                    name: "FK_stock_movements_raw_materials_raw_material_id",
                    column: x => x.raw_material_id,
                    principalTable: "raw_materials",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_production_tasks_order_id", table: "production_tasks", column: "order_id");
        migrationBuilder.CreateIndex(name: "IX_production_tasks_status", table: "production_tasks", column: "status");
        migrationBuilder.CreateIndex(name: "IX_stock_movements_raw_material_id", table: "stock_movements", column: "raw_material_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "stock_movements");
        migrationBuilder.DropTable(name: "production_tasks");
        migrationBuilder.DropTable(name: "raw_materials");
    }
}
