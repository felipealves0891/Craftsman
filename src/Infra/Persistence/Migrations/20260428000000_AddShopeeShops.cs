using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

public partial class AddShopeeShops : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "shopee_shops",
            columns: table => new
            {
                shop_id = table.Column<long>(type: "bigint", nullable: false),
                access_token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                refresh_token = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                access_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                authorization_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shopee_shops", x => x.shop_id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_shopee_shops_authorization_status",
            table: "shopee_shops",
            column: "authorization_status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "shopee_shops");
    }
}
