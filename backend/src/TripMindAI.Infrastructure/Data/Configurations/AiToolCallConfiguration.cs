using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripMindAI.Domain.Entities;

namespace TripMindAI.Infrastructure.Data.Configurations;

public class AiToolCallConfiguration : IEntityTypeConfiguration<AiToolCall>
{
    public void Configure(EntityTypeBuilder<AiToolCall> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ToolName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.RequestJson)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(t => t.ResponseJson)
            .HasMaxLength(8000);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(t => t.Message)
            .WithMany(m => m.ToolCalls)
            .HasForeignKey(t => t.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
