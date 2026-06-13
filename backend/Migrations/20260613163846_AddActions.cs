using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    submitted_at_tick = table.Column<int>(type: "integer", nullable: false),
                    ready_at_tick = table.Column<int>(type: "integer", nullable: true),
                    duration_ticks = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_actions_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_actions_city_id_status",
                table: "actions",
                columns: new[] { "city_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_actions_status_ready_at_tick",
                table: "actions",
                columns: new[] { "status", "ready_at_tick" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actions");
        }
    }
}
