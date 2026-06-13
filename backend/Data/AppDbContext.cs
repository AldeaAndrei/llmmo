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

    public DbSet<User> Users => Set<User>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<Building> Buildings => Set<Building>();

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
            entity.Property(player => player.OwnerUserId).HasColumnName("owner_user_id");
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

            entity.HasOne(player => player.OwnerUser)
                .WithMany(user => user.Players)
                .HasForeignKey(player => player.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Email)
                .HasColumnName("email")
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(user => user.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();
            entity.Property(user => user.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(user => user.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("api_keys");

            entity.HasKey(key => key.Id);

            entity.Property(key => key.Id).HasColumnName("id");
            entity.Property(key => key.PlayerId).HasColumnName("player_id");
            entity.Property(key => key.KeyHash)
                .HasColumnName("key_hash")
                .IsRequired();
            entity.Property(key => key.KeyPrefix)
                .HasColumnName("key_prefix")
                .HasMaxLength(16)
                .IsRequired();
            entity.Property(key => key.Label)
                .HasColumnName("label")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(key => key.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(key => key.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
            entity.Property(key => key.RevokedAt)
                .HasColumnName("revoked_at");
            entity.Property(key => key.LastUsedAt)
                .HasColumnName("last_used_at");

            entity.HasIndex(key => key.PlayerId);
            entity.HasIndex(key => key.KeyPrefix).IsUnique();

            entity.HasOne(key => key.Player)
                .WithMany(player => player.ApiKeys)
                .HasForeignKey(key => key.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Building>(entity =>
        {
            entity.ToTable("buildings");

            entity.HasKey(building => building.Id);

            entity.Property(building => building.Id).HasColumnName("id");
            entity.Property(building => building.CityId).HasColumnName("city_id");
            entity.Property(building => building.Type)
                .HasColumnName("type")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(building => building.Level)
                .HasColumnName("level")
                .HasDefaultValue(1)
                .IsRequired();
            entity.Property(building => building.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(building => building.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(building => new { building.CityId, building.Type }).IsUnique();

            entity.HasOne(building => building.City)
                .WithMany(city => city.Buildings)
                .HasForeignKey(building => building.CityId)
                .OnDelete(DeleteBehavior.Cascade);
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

        foreach (var entry in ChangeTracker.Entries<User>())
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

        foreach (var entry in ChangeTracker.Entries<ApiKey>())
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

        foreach (var entry in ChangeTracker.Entries<Building>())
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
