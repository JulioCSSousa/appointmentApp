using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentAppMvc.Migrations
{
    /// <inheritdoc />
    public partial class appointmentSnapShot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "Appointments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ServicePrice",
                table: "Appointments",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ServicePrice",
                table: "Appointments");
        }
    }
}
