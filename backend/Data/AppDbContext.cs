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

    public DbSet<CityTroop> CityTroops => Set<CityTroop>();

    public DbSet<MilitaryAttack> MilitaryAttacks => Set<MilitaryAttack>();

    public DbSet<Report> Reports => Set<Report>();

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
            entity.Property(city => city.MaxWood)
                .HasColumnName("max_wood")
                .HasDefaultValue(1000)
                .IsRequired();
            entity.Property(city => city.MaxStone)
                .HasColumnName("max_stone")
                .HasDefaultValue(1000)
                .IsRequired();
            entity.Property(city => city.MaxGold)
                .HasColumnName("max_gold")
                .HasDefaultValue(1000)
                .IsRequired();
            entity.Property(city => city.MaxFood)
                .HasColumnName("max_food")
                .HasDefaultValue(1000)
                .IsRequired();
            entity.Property(city => city.DefenceFactor)
                .HasColumnName("defence_factor")
                .HasDefaultValue(1.0)
                .IsRequired();
            entity.Property(city => city.SpyDieChance)
                .HasColumnName("spy_die_chance")
                .HasDefaultValue(0.5)
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

        modelBuilder.Entity<CityTroop>(entity =>
        {
            entity.ToTable("city_troops");

            entity.HasKey(troop => troop.Id);

            entity.Property(troop => troop.Id).HasColumnName("id");
            entity.Property(troop => troop.CityId).HasColumnName("city_id");
            entity.Property(troop => troop.Type)
                .HasColumnName("type")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(troop => troop.Quantity)
                .HasColumnName("quantity")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(troop => troop.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(troop => troop.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(troop => new { troop.CityId, troop.Type }).IsUnique();

            entity.HasOne(troop => troop.City)
                .WithMany(city => city.Troops)
                .HasForeignKey(troop => troop.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MilitaryAttack>(entity =>
        {
            entity.ToTable("attacks");

            entity.HasKey(attack => attack.Id);

            entity.Property(attack => attack.Id).HasColumnName("id");
            entity.Property(attack => attack.PlayerId).HasColumnName("player_id");
            entity.Property(attack => attack.SourceCityId).HasColumnName("source_city_id");
            entity.Property(attack => attack.TargetCityId).HasColumnName("target_city_id");
            entity.Property(attack => attack.TargetX).HasColumnName("target_x").IsRequired();
            entity.Property(attack => attack.TargetY).HasColumnName("target_y").IsRequired();
            entity.Property(attack => attack.Type)
                .HasColumnName("type")
                .HasMaxLength(16)
                .IsRequired();
            entity.Property(attack => attack.Status)
                .HasColumnName("status")
                .HasMaxLength(16)
                .IsRequired();
            entity.Property(attack => attack.Troops)
                .HasColumnName("troops")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(attack => attack.Survivors)
                .HasColumnName("survivors")
                .HasColumnType("jsonb");
            entity.Property(attack => attack.OutboundDurationTicks)
                .HasColumnName("outbound_duration_ticks")
                .IsRequired();
            entity.Property(attack => attack.ReturnDurationTicks)
                .HasColumnName("return_duration_ticks")
                .IsRequired();
            entity.Property(attack => attack.DepartedAtTick)
                .HasColumnName("departed_at_tick")
                .IsRequired();
            entity.Property(attack => attack.ArrivesAtTick)
                .HasColumnName("arrives_at_tick")
                .IsRequired();
            entity.Property(attack => attack.ReturnsAtTick)
                .HasColumnName("returns_at_tick");
            entity.Property(attack => attack.LootWood).HasColumnName("loot_wood").IsRequired();
            entity.Property(attack => attack.LootStone).HasColumnName("loot_stone").IsRequired();
            entity.Property(attack => attack.LootGold).HasColumnName("loot_gold").IsRequired();
            entity.Property(attack => attack.LootFood).HasColumnName("loot_food").IsRequired();
            entity.Property(attack => attack.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(attack => attack.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(attack => attack.PlayerId);
            entity.HasIndex(attack => attack.Status);

            entity.HasOne(attack => attack.Player)
                .WithMany()
                .HasForeignKey(attack => attack.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(attack => attack.SourceCity)
                .WithMany()
                .HasForeignKey(attack => attack.SourceCityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(attack => attack.TargetCity)
                .WithMany()
                .HasForeignKey(attack => attack.TargetCityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("reports");

            entity.HasKey(report => report.Id);

            entity.Property(report => report.Id).HasColumnName("id");
            entity.Property(report => report.PlayerId).HasColumnName("player_id");
            entity.Property(report => report.Type)
                .HasColumnName("type")
                .HasMaxLength(16)
                .IsRequired();
            entity.Property(report => report.AttackId).HasColumnName("attack_id");
            entity.Property(report => report.SourceCityId).HasColumnName("source_city_id");
            entity.Property(report => report.TargetCityId).HasColumnName("target_city_id");
            entity.Property(report => report.TargetX).HasColumnName("target_x").IsRequired();
            entity.Property(report => report.TargetY).HasColumnName("target_y").IsRequired();
            entity.Property(report => report.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(report => report.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            entity.Property(report => report.ReadAt)
                .HasColumnName("read_at");

            entity.HasIndex(report => report.PlayerId);

            entity.HasOne(report => report.Player)
                .WithMany()
                .HasForeignKey(report => report.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(report => report.Attack)
                .WithMany()
                .HasForeignKey(report => report.AttackId)
                .OnDelete(DeleteBehavior.SetNull);
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
            entity.Property(state => state.WorldSeed)
                .HasColumnName("world_seed")
                .HasDefaultValue(1)
                .IsRequired();
            entity.Property(state => state.MapSize)
                .HasColumnName("map_size")
                .HasDefaultValue(100)
                .IsRequired();
            entity.Property(state => state.LastTickAt)
                .HasColumnName("last_tick_at")
                .IsRequired();
            entity.Property(state => state.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasData(new WorldState
            {
                Id = 1,
                CurrentTick = 0,
                WorldSeed = 1,
                MapSize = 100,
                LastTickAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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

        foreach (var entry in ChangeTracker.Entries<CityTroop>())
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

        foreach (var entry in ChangeTracker.Entries<MilitaryAttack>())
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

        foreach (var entry in ChangeTracker.Entries<Report>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
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
