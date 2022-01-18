using Microsoft.EntityFrameworkCore.Migrations;

namespace UserRepository.Migrations
{
    public partial class Add_ChartSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChartSettings",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChartSettings",
                table: "Users");
        }
    }
}
