using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniShare.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisputeResolution",
                table: "Rides",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisputeResolution",
                table: "Rides");
        }
    }
}
