using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class ApplyBuildingEffectsAndDropDefenceFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE cities c
                SET
                    max_wood = 1000 + 150 * COALESCE(s.level, 0),
                    max_stone = 1000 + 150 * COALESCE(s.level, 0),
                    max_gold = 1000 + 150 * COALESCE(s.level, 0),
                    max_food = 1000 + 150 * COALESCE(s.level, 0)
                FROM (
                    SELECT city_id, level
                    FROM buildings
                    WHERE type = 'storage_shed'
                ) AS s
                WHERE c.id = s.city_id;
                """);

            migrationBuilder.Sql(
                """
                UPDATE cities c
                SET spy_die_chance = 1.0 - LEAST(
                    0.80,
                    0.50 + 0.015 * GREATEST(COALESCE(a.level, 1) - 1, 0)
                )
                FROM (
                    SELECT city_id, level
                    FROM buildings
                    WHERE type = 'spy_academy'
                ) AS a
                WHERE c.id = a.city_id;
                """);

            migrationBuilder.Sql(
                """
                UPDATE cities
                SET
                    wood = LEAST(wood, max_wood),
                    stone = LEAST(stone, max_stone),
                    gold = LEAST(gold, max_gold),
                    food = LEAST(food, max_food);
                """);

            migrationBuilder.DropColumn(
                name: "defence_factor",
                table: "cities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "defence_factor",
                table: "cities",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.Sql(
                """
                UPDATE cities
                SET
                    max_wood = 1000,
                    max_stone = 1000,
                    max_gold = 1000,
                    max_food = 1000,
                    spy_die_chance = 0.5;
                """);
        }
    }
}
