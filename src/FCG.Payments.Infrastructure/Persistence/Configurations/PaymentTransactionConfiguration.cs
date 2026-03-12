using FCG.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Payments.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PurchaseId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.GameId)
            .HasMaxLength(200);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(200);

        builder.HasIndex(x => x.PurchaseId);
        builder.HasIndex(x => x.UserId);
    }
}
