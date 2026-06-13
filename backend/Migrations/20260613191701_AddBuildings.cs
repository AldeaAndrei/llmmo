using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buildings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildings", x => x.id);
                    table.ForeignKey(
                        name: "FK_buildings_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_buildings_city_id_type",
                table: "buildings",
                columns: new[] { "city_id", "type" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO buildings (id, city_id, type, level, created_at, updated_at)
                SELECT gen_random_uuid(), c.id, t.type, 1, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
                FROM cities c
                CROSS JOIN (
                    VALUES
                        ('gold_mine'),
                        ('stone_mine'),
                        ('timber_station'),
                        ('bakery'),
                        ('barracks')
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
            migrationBuilder.DropTable(
                name: "buildings");
        }
    }
}
