using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceholderBuildings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO buildings (id, city_id, type, level, created_at, updated_at)
                SELECT gen_random_uuid(), c.id, t.type, 1, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
                FROM cities c
                CROSS JOIN (
                    VALUES
                        ('storage_shed'),
                        ('spy_academy'),
                        ('wall')
                ) AS t(type)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM buildings b
                    WHERE b.city_id = c.id AND b.type = t.type
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM buildings
                WHERE type IN ('storage_shed', 'spy_academy', 'wall');
                """);
        }
    }
}
