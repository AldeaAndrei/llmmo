using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddDiplomacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_diplomacy_declared_at_tick",
                table: "players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "last_message_sent_at_tick",
                table: "players",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "diplomacy_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    declared_by_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_tick = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diplomacy_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_diplomacy_events_players_declared_by_player_id",
                        column: x => x.declared_by_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_diplomacy_events_players_target_player_id",
                        column: x => x.target_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at_tick = table.Column<int>(type: "integer", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_player_messages_players_from_player_id",
                        column: x => x.from_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_messages_players_to_player_id",
                        column: x => x.to_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "player_relations",
                columns: table => new
                {
                    from_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_tick = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_relations", x => new { x.from_player_id, x.to_player_id });
                    table.ForeignKey(
                        name: "FK_player_relations_players_from_player_id",
                        column: x => x.from_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_player_relations_players_to_player_id",
                        column: x => x.to_player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diplomacy_events_created_at",
                table: "diplomacy_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_diplomacy_events_declared_by_player_id",
                table: "diplomacy_events",
                column: "declared_by_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_diplomacy_events_target_player_id",
                table: "diplomacy_events",
                column: "target_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_messages_from_player_id_sent_at",
                table: "player_messages",
                columns: new[] { "from_player_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_player_messages_to_player_id_sent_at",
                table: "player_messages",
                columns: new[] { "to_player_id", "sent_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "diplomacy_events");
            migrationBuilder.DropTable(name: "player_messages");
            migrationBuilder.DropTable(name: "player_relations");

            migrationBuilder.DropColumn(
                name: "last_diplomacy_declared_at_tick",
                table: "players");

            migrationBuilder.DropColumn(
                name: "last_message_sent_at_tick",
                table: "players");
        }
    }
}
