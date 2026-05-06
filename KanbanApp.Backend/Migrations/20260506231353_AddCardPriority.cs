using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanApp.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Cards");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Cards",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Cards");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Cards",
                type: "text",
                nullable: true);
        }
    }
}
