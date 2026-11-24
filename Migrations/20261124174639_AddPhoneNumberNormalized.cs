using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentAppMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneNumberNormalized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumberNormalized",
                table: "AspNetUsers",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PhoneNumberNormalized",
                table: "AspNetUsers",
                column: "PhoneNumberNormalized",
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PhoneNumberNormalized",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumberNormalized",
                table: "AspNetUsers");

        }
    }
}
