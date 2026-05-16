using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniShare.Migrations
{
    /// <inheritdoc />
    public partial class RideStatusNameChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ridetatus",
                table: "Rides",
                newName: "RideStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RideStatus",
                table: "Rides",
                newName: "Ridetatus");
        }
    }
}
