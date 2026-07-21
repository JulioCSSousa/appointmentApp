using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentAppMvc.Migrations
{
    /// <inheritdoc />
    public partial class workerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkerId",
                table: "Appointments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WorkerId",
                table: "WorkSchedules",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "WorkSchedules");
        }
    }
}
