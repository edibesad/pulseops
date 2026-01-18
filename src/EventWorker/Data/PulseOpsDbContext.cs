using System.Data.Common;
using EventWorker.Persistence.Entities;
using Microsoft.EntityFrameworkCore;


public class PulseOpsDbContext : DbContext
{
    public PulseOpsDbContext(DbContextOptions<PulseOpsDbContext> options) : base(options) { }

    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IncidentEventEntity>(
            e =>
            {
                e.ToTable("incident_events");
                e.HasKey(x => x.Id);

                e.Property(x => x.EventType).HasColumnName("event_type").IsRequired();
                e.Property(x => x.PayloadJson).HasColumnName("payload_json").IsRequired();
                e.Property(x => x.ReceivedAt).HasColumnName("received_at").IsRequired();

                e.Property(x => x.CorrelationId).HasColumnName("correlation_id");
                e.Property(x => x.IncidentKey).HasColumnName("incident_key");

                e.HasIndex(x => x.IncidentKey);
                e.HasIndex(x => x.ReceivedAt);
            }
        );
        modelBuilder.Entity<Incident>(b =>
            {
                b.ToTable("incidents");
                b.HasKey(x => x.Id);

                b.Property(x => x.IncidentKey).HasColumnName("incident_key").IsRequired();
                b.HasIndex(x => x.IncidentKey).IsUnique();

                b.Property(x => x.StartedAt).HasColumnName("started_at").IsRequired();
                b.Property(x => x.EndedAt).HasColumnName("ended_at");
                b.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");

                b.Property(x => x.Status).HasColumnName("status").HasConversion<int>();

                b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
                b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
            });
    }
}