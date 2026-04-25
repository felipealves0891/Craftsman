using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

public partial class AddShipments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "shipments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                tracking_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                shipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shipments", x => x.id);
            });

        migrationBuilder.CreateIndex(name: "IX_shipments_order_id", table: "shipments", column: "order_id");
        migrationBuilder.CreateIndex(name: "IX_shipments_status", table: "shipments", column: "status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "shipments");
    }
}
