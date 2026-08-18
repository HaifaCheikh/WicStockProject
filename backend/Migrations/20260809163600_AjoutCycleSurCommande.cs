using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCycleSurCommande : Migration
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
                name: "Statut",
                table: "HistoriqueVentes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisponibleSurCommande",
                table: "Produits");

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
                name: "DatePrete",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "DateSouhaitee",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "EstSurCommande",
                table: "HistoriqueVentes");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "HistoriqueVentes");
        }
    }
}
