using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AjoutUtilisateurCommande : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId",
                table: "HistoriqueVentes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueVentes_UtilisateurId",
                table: "HistoriqueVentes",
                column: "UtilisateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoriqueVentes_Utilisateurs_UtilisateurId",
                table: "HistoriqueVentes",
                column: "UtilisateurId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoriqueVentes_Utilisateurs_UtilisateurId",
                table: "HistoriqueVentes");

            migrationBuilder.DropIndex(
                name: "IX_HistoriqueVentes_UtilisateurId",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "UtilisateurId",
                table: "HistoriqueVentes");
        }
    }
}
