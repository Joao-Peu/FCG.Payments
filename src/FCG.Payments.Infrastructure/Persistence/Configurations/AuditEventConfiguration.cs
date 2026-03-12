using FCG.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Payments.Infrastructure.Persistence.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AggregateType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AggregateId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(200);

        builder.Property(x => x.TraceId)
            .HasMaxLength(200);

        builder.Property(x => x.UserId)
            .HasMaxLength(200);

        builder.HasIndex(x => x.AggregateId);
    }
}
