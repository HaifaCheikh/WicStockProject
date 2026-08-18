using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CS8981

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class miseajour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "Utilisateurs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Utilisateurs");
        }
    }
}
