using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerIdToActionsAndWorldState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "player_id",
                table: "actions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "world_state",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    current_tick = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_state", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "world_state",
                columns: new[] { "id", "updated_at" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_actions_player_id",
                table: "actions",
                column: "player_id");

            migrationBuilder.AddForeignKey(
                name: "FK_actions_players_player_id",
                table: "actions",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_actions_players_player_id",
                table: "actions");

            migrationBuilder.DropTable(
                name: "world_state");

            migrationBuilder.DropIndex(
                name: "IX_actions_player_id",
                table: "actions");

            migrationBuilder.DropColumn(
                name: "player_id",
                table: "actions");
        }
    }
}
