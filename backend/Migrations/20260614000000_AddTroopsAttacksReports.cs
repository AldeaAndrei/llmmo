using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddTroopsAttacksReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "defence_factor",
                table: "cities",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "spy_die_chance",
                table: "cities",
                type: "double precision",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.CreateTable(
                name: "city_troops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_city_troops", x => x.id);
                    table.ForeignKey(
                        name: "FK_city_troops_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_x = table.Column<int>(type: "integer", nullable: false),
                    target_y = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    troops = table.Column<string>(type: "jsonb", nullable: false),
                    survivors = table.Column<string>(type: "jsonb", nullable: true),
                    outbound_duration_ticks = table.Column<int>(type: "integer", nullable: false),
                    return_duration_ticks = table.Column<int>(type: "integer", nullable: false),
                    departed_at_tick = table.Column<int>(type: "integer", nullable: false),
                    arrives_at_tick = table.Column<int>(type: "integer", nullable: false),
                    returns_at_tick = table.Column<int>(type: "integer", nullable: true),
                    loot_wood = table.Column<int>(type: "integer", nullable: false),
                    loot_stone = table.Column<int>(type: "integer", nullable: false),
                    loot_gold = table.Column<int>(type: "integer", nullable: false),
                    loot_food = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attacks", x => x.id);
                    table.ForeignKey(
                        name: "FK_attacks_cities_source_city_id",
                        column: x => x.source_city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attacks_cities_target_city_id",
                        column: x => x.target_city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attacks_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    attack_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_x = table.Column<int>(type: "integer", nullable: false),
                    target_y = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_reports_attacks_attack_id",
                        column: x => x.attack_id,
                        principalTable: "attacks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reports_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_city_troops_city_id_type",
                table: "city_troops",
                columns: new[] { "city_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attacks_player_id",
                table: "attacks",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_attacks_status",
                table: "attacks",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_reports_player_id",
                table: "reports",
                column: "player_id");

            migrationBuilder.Sql(
                """
                INSERT INTO city_troops (id, city_id, type, quantity, created_at, updated_at)
                SELECT gen_random_uuid(), c.id, t.type, 0, NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC'
                FROM cities c
                CROSS JOIN (VALUES ('soldier'), ('spy')) AS t(type)
                WHERE NOT EXISTS (
                    SELECT 1 FROM city_troops ct
                    WHERE ct.city_id = c.id AND ct.type = t.type
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE city_troops ct
                SET quantity = c.troop_count
                FROM cities c
                WHERE ct.city_id = c.id AND ct.type = 'soldier' AND c.troop_count > 0;
                """);

            migrationBuilder.DropColumn(
                name: "troop_count",
                table: "cities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "troop_count",
                table: "cities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE cities c
                SET troop_count = COALESCE(
                    (SELECT quantity FROM city_troops ct WHERE ct.city_id = c.id AND ct.type = 'soldier'),
                    0);
                """);

            migrationBuilder.DropTable(name: "reports");
            migrationBuilder.DropTable(name: "attacks");
            migrationBuilder.DropTable(name: "city_troops");

            migrationBuilder.DropColumn(name: "defence_factor", table: "cities");
            migrationBuilder.DropColumn(name: "spy_die_chance", table: "cities");
        }
    }
}
