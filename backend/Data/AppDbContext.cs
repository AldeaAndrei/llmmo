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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var playerTypeConverter = new ValueConverter<PlayerType, string>(
            value => value == PlayerType.Human ? "human" : "llm",
            value => value == "human" ? PlayerType.Human : PlayerType.Llm);

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
    }
}
