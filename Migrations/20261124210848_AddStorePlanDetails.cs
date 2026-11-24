using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentAppMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddStorePlanDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivePlan",
                table: "Stores",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Free")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanExpiresAt",
                table: "Stores",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanStatus",
                table: "Stores",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Active")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivePlan",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "PlanExpiresAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "PlanStatus",
                table: "Stores");
        }
    }
}
