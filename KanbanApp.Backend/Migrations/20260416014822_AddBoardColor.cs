using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanApp.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Boards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Boards");
        }
    }
}
