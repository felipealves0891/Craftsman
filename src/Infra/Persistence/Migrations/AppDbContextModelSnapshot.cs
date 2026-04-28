using Craftsman.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Craftsman.Infra.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("Craftsman.Infra.Persistence.Entities.PersistedDomainEventEntity", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid")
                .HasColumnName("id");

            b.Property<string>("EventName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("event_name");

            b.Property<DateTimeOffset>("OccurredAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("occurred_at");

            b.Property<string>("Payload")
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnName("payload");

            b.HasKey("Id");

            b.ToTable("domain_events");
        });

        modelBuilder.Entity("Craftsman.Infra.Persistence.Entities.ShopeeShopEntity", b =>
        {
            b.Property<long>("ShopId")
                .HasColumnType("bigint")
                .HasColumnName("shop_id");

            b.Property<string>("AccessToken")
                .IsRequired()
                .HasMaxLength(4096)
                .HasColumnType("character varying(4096)")
                .HasColumnName("access_token");

            b.Property<DateTimeOffset>("AccessTokenExpiresAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("access_token_expires_at");

            b.Property<string>("AuthorizationStatus")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("character varying(50)")
                .HasColumnName("authorization_status");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");

            b.Property<string>("RefreshToken")
                .IsRequired()
                .HasMaxLength(4096)
                .HasColumnType("character varying(4096)")
                .HasColumnName("refresh_token");

            b.Property<DateTimeOffset>("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            b.HasKey("ShopId");

            b.HasIndex("AuthorizationStatus");

            b.ToTable("shopee_shops");
        });
    }
}
