using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

public partial class AddSalesAndProductCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "orders",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                external_order_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                customer_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_orders", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "products",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_products", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "order_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                external_item_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_order_items", x => x.id);
                table.ForeignKey(
                    name: "FK_order_items_orders_order_id",
                    column: x => x.order_id,
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "bill_of_materials_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false),
                raw_material_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity_per_unit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bill_of_materials_items", x => x.id);
                table.ForeignKey(
                    name: "FK_bill_of_materials_items_products_product_id",
                    column: x => x.product_id,
                    principalTable: "products",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "product_mappings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                external_item_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                product_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_product_mappings", x => x.id);
                table.ForeignKey(
                    name: "FK_product_mappings_products_product_id",
                    column: x => x.product_id,
                    principalTable: "products",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_orders_source_external_order_id", table: "orders", columns: new[] { "source", "external_order_id" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_order_items_order_id", table: "order_items", column: "order_id");
        migrationBuilder.CreateIndex(name: "IX_bill_of_materials_items_product_id_raw_material_id", table: "bill_of_materials_items", columns: new[] { "product_id", "raw_material_id" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_product_mappings_product_id", table: "product_mappings", column: "product_id");
        migrationBuilder.CreateIndex(name: "IX_product_mappings_source_external_item_id", table: "product_mappings", columns: new[] { "source", "external_item_id" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bill_of_materials_items");
        migrationBuilder.DropTable(name: "order_items");
        migrationBuilder.DropTable(name: "product_mappings");
        migrationBuilder.DropTable(name: "orders");
        migrationBuilder.DropTable(name: "products");
    }
}
