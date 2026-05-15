using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanApp.Backend.Migrations
{
    public partial class AddObjectPositions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverObjectPosition",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverObjectPosition",
                table: "Boards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectPosition",
                table: "CardImages",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CoverObjectPosition", table: "Projects");
            migrationBuilder.DropColumn(name: "CoverObjectPosition", table: "Boards");
            migrationBuilder.DropColumn(name: "ObjectPosition", table: "CardImages");
        }
    }
}
