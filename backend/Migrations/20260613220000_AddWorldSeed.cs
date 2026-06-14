using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "map_size",
                table: "world_state",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "world_seed",
                table: "world_state",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.UpdateData(
                table: "world_state",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "map_size", "world_seed" },
                values: new object[] { 100, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "map_size",
                table: "world_state");

            migrationBuilder.DropColumn(
                name: "world_seed",
                table: "world_state");
        }
    }
}
