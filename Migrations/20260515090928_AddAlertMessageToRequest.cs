using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniShare.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertMessageToRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertMessage",
                table: "RideRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertMessage",
                table: "RideRequests");
        }
    }
}
