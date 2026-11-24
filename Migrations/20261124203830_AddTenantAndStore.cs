using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentAppMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAndStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "WorkSchedules",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Services",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "AspNetUsers",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Appointments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stores_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_StoreId",
                table: "WorkSchedules",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_StoreId",
                table: "Services",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StoreId",
                table: "AspNetUsers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StoreId",
                table: "Appointments",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Slug",
                table: "Stores",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_TenantId",
                table: "Stores",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Stores_StoreId",
                table: "Appointments",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Stores_StoreId",
                table: "AspNetUsers",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Stores_StoreId",
                table: "Services",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSchedules_Stores_StoreId",
                table: "WorkSchedules",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Stores_StoreId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Stores_StoreId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Stores_StoreId",
                table: "Services");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkSchedules_Stores_StoreId",
                table: "WorkSchedules");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_StoreId",
                table: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Services_StoreId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StoreId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_StoreId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Appointments");
        }
    }
}
