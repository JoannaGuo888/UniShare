using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniShare.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedReportNavProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_ReporterUserUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_SubjectUserUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterUserUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_SubjectUserUserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReporterUserUserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "SubjectUserUserId",
                table: "Reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReporterUserUserId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubjectUserUserId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUserUserId",
                table: "Reports",
                column: "ReporterUserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SubjectUserUserId",
                table: "Reports",
                column: "SubjectUserUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_ReporterUserUserId",
                table: "Reports",
                column: "ReporterUserUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_SubjectUserUserId",
                table: "Reports",
                column: "SubjectUserUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
