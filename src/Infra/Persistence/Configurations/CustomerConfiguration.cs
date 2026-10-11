using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id).HasColumnName("id");
        builder.Property(customer => customer.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.NormalizedName).HasColumnName("normalized_name").HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.Phone).HasColumnName("phone").HasMaxLength(50);
        builder.Property(customer => customer.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(customer => customer.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(customer => customer.NormalizedName).IsUnique();
    }
}
