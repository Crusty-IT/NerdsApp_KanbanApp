using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanApp.Backend.Migrations
{
    public partial class AddProjectAndBoardCovers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Boards",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Boards");
        }
    }
}
