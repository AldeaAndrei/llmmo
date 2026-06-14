using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace llmmo.Migrations
{
    /// <inheritdoc />
    public partial class AddCityResourceCaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_food",
                table: "cities",
                type: "integer",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<int>(
                name: "max_gold",
                table: "cities",
                type: "integer",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<int>(
                name: "max_stone",
                table: "cities",
                type: "integer",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<int>(
                name: "max_wood",
                table: "cities",
                type: "integer",
                nullable: false,
                defaultValue: 1000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_food",
                table: "cities");

            migrationBuilder.DropColumn(
                name: "max_gold",
                table: "cities");

            migrationBuilder.DropColumn(
                name: "max_stone",
                table: "cities");

            migrationBuilder.DropColumn(
                name: "max_wood",
                table: "cities");
        }
    }
}
