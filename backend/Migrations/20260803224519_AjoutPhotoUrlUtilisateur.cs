using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPhotoUrlUtilisateur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Utilisateurs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Utilisateurs");
        }
    }
}
