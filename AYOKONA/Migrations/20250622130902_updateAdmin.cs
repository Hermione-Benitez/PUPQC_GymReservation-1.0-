using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AYOKONA.Migrations
{
    /// <inheritdoc />
    public partial class updateAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reservation_id",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AdminAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "AdminAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_reservation_id",
                table: "Requests",
                column: "reservation_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Reservations_reservation_id",
                table: "Requests",
                column: "reservation_id",
                principalTable: "Reservations",
                principalColumn: "reservation_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Reservations_reservation_id",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_reservation_id",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "reservation_id",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AdminAccounts");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "AdminAccounts");
        }
    }
}
