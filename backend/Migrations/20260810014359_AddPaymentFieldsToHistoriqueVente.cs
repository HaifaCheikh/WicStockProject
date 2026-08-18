using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFieldsToHistoriqueVente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DisponibleSurCommande",
                table: "Produits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurDestinataireId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateConfirmation",
                table: "HistoriqueVentes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDebutPreparation",
                table: "HistoriqueVentes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateEstimeePreparation",
                table: "HistoriqueVentes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLivraison",
                table: "HistoriqueVentes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePaiement",
                table: "HistoriqueVentes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePrete",
                table: "HistoriqueVentes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateSouhaitee",
                table: "HistoriqueVentes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstSurCommande",
                table: "HistoriqueVentes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentIntentId",
                table: "HistoriqueVentes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsableId",
                table: "HistoriqueVentes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Statut",
                table: "HistoriqueVentes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueVentes_ResponsableId",
                table: "HistoriqueVentes",
                column: "ResponsableId");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoriqueVentes_Utilisateurs_ResponsableId",
                table: "HistoriqueVentes",
                column: "ResponsableId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HistoriqueVentes_Utilisateurs_ResponsableId",
                table: "HistoriqueVentes");

            migrationBuilder.DropIndex(
                name: "IX_HistoriqueVentes_ResponsableId",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DisponibleSurCommande",
                table: "Produits");

            migrationBuilder.DropColumn(
                name: "UtilisateurDestinataireId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DateConfirmation",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DateDebutPreparation",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DateEstimeePreparation",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DateLivraison",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DatePaiement",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DatePrete",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DateSouhaitee",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "EstSurCommande",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "ResponsableId",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "HistoriqueVentes");
        }
    }
}
