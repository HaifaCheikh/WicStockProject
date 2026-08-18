using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPrixUnitaireProduit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrixUnitaire",
                table: "Produits",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrixUnitaire",
                table: "Produits");
        }
    }
}
