using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure;

public class OrderMapConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("TB_ORDER")
            .HasKey(k => k.Id);
        builder.Property(e => e.OrderDate)
            .IsRequired();
        builder.Property(e => e.Total)
            .IsRequired()
            .HasPrecision(18, 2);
        builder.HasOne(e => e.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(e => e.CustomerId);
    }
}