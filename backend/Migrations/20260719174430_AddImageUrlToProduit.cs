using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToProduit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Produits",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Produits");
        }
    }
}
