using llmmo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace llmmo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<GameAction> Actions => Set<GameAction>();

    public DbSet<WorldState> WorldState => Set<WorldState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var playerTypeConverter = new ValueConverter<PlayerType, string>(
            value => value == PlayerType.Human ? "human" : "llm",
            value => value == "human" ? PlayerType.Human : PlayerType.Llm);

        var actionStatusConverter = new ValueConverter<ActionStatus, string>(
            value => ActionStatusToString(value),
            value => StringToActionStatus(value));

        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("players");

            entity.HasKey(player => player.Id);

            entity.Property(player => player.Id).HasColumnName("id");
            entity.Property(player => player.Name)
                .HasColumnName("name")
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(player => player.PlayerType)
                .HasColumnName("player_type")
                .HasMaxLength(16)
                .HasConversion(playerTypeConverter)
                .IsRequired();
            entity.Property(player => player.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(player => player.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("cities");

            entity.HasKey(city => city.Id);

            entity.Property(city => city.Id).HasColumnName("id");
            entity.Property(city => city.PlayerId).HasColumnName("player_id");
            entity.Property(city => city.X).HasColumnName("x").IsRequired();
            entity.Property(city => city.Y).HasColumnName("y").IsRequired();
            entity.Property(city => city.Name)
                .HasColumnName("name")
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(city => city.Wood)
                .HasColumnName("wood")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(city => city.Stone)
                .HasColumnName("stone")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(city => city.Gold)
                .HasColumnName("gold")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(city => city.Food)
                .HasColumnName("food")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(city => city.TroopCount)
                .HasColumnName("troop_count")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(city => city.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(city => city.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(city => new { city.X, city.Y }).IsUnique();

            entity.HasOne(city => city.Player)
                .WithMany(player => player.Cities)
                .HasForeignKey(city => city.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GameAction>(entity =>
        {
            entity.ToTable("actions");

            entity.HasKey(action => action.Id);

            entity.Property(action => action.Id).HasColumnName("id");
            entity.Property(action => action.PlayerId).HasColumnName("player_id");
            entity.Property(action => action.CityId).HasColumnName("city_id");
            entity.Property(action => action.Type)
                .HasColumnName("type")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(action => action.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(action => action.Status)
                .HasColumnName("status")
                .HasMaxLength(16)
                .HasConversion(actionStatusConverter)
                .IsRequired();
            entity.Property(action => action.SubmittedAtTick)
                .HasColumnName("submitted_at_tick")
                .IsRequired();
            entity.Property(action => action.ReadyAtTick)
                .HasColumnName("ready_at_tick");
            entity.Property(action => action.DurationTicks)
                .HasColumnName("duration_ticks")
                .IsRequired();
            entity.Property(action => action.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(action => action.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(action => new { action.Status, action.ReadyAtTick });
            entity.HasIndex(action => new { action.CityId, action.Status });

            entity.HasOne(action => action.City)
                .WithMany(city => city.Actions)
                .HasForeignKey(action => action.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(action => action.Player)
                .WithMany(player => player.Actions)
                .HasForeignKey(action => action.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorldState>(entity =>
        {
            entity.ToTable("world_state");

            entity.HasKey(state => state.Id);

            entity.Property(state => state.Id).HasColumnName("id");
            entity.Property(state => state.CurrentTick)
                .HasColumnName("current_tick")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(state => state.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasData(new WorldState
            {
                Id = 1,
                CurrentTick = 0,
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        });
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Player>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<City>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<GameAction>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<WorldState>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }

    private static string ActionStatusToString(ActionStatus status) => status switch
    {
        ActionStatus.Queued => "queued",
        ActionStatus.InProgress => "in_progress",
        ActionStatus.Done => "done",
        ActionStatus.Failed => "failed",
        _ => "queued",
    };

    private static ActionStatus StringToActionStatus(string status) => status switch
    {
        "queued" => ActionStatus.Queued,
        "in_progress" => ActionStatus.InProgress,
        "done" => ActionStatus.Done,
        "failed" => ActionStatus.Failed,
        _ => ActionStatus.Queued,
    };
}
