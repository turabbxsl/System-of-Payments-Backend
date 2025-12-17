using Microsoft.EntityFrameworkCore;

namespace PaymentsApi.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Eventlog> Eventlogs { get; set; }

    public virtual DbSet<Idempotencykey> Idempotencykeys { get; set; }

    public virtual DbSet<Outboxmessage> Outboxmessages { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum("transactionstatus", new[] { "Pending", "Processing", "Success", "Failed", "New" });

        modelBuilder.Entity<Eventlog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("eventlog_pkey");

            entity.ToTable("eventlog");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Eventtype)
                .HasMaxLength(100)
                .HasColumnName("eventtype");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.Transactionid).HasColumnName("transactionid");
        });

        modelBuilder.Entity<Idempotencykey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("idempotencykeys_pkey");

            entity.ToTable("idempotencykeys");

            entity.HasIndex(e => e.Key, "idempotencykeys_key_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Key)
                .HasMaxLength(200)
                .HasColumnName("key");
            entity.Property(e => e.Requesthash)
                .HasMaxLength(200)
                .HasColumnName("requesthash");
            entity.Property(e => e.Resultdata)
                .HasColumnType("jsonb")
                .HasColumnName("resultdata");
            entity.Property(e => e.Updatedat).HasColumnName("updatedat");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<Outboxmessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("outboxmessages_pkey");

            entity.ToTable("outboxmessages");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.Transactionid).HasColumnName("transactionid");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");

            entity.Property(e => e.Status).HasColumnType("transactionstatus");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .HasColumnName("currency");
            entity.Property(e => e.Providerid).HasColumnName("providerid");
            entity.Property(e => e.Updatedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.Property(e => e.Status).HasColumnType("transactionstatus");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
