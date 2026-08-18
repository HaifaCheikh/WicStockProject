using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPromotionProduit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId",
                table: "ActionsRecommandees");

            migrationBuilder.DropIndex(
                name: "IX_ActionsRecommandees_PrevisionEtatProduitId",
                table: "ActionsRecommandees");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFinPromotion",
                table: "Produits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemisePourcentage",
                table: "Produits",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PrevisionEtatProduitId",
                table: "ActionsRecommandees",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_ActionsRecommandees_PrevisionEtatProduitId",
                table: "ActionsRecommandees",
                column: "PrevisionEtatProduitId",
                unique: true,
                filter: "[PrevisionEtatProduitId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId",
                table: "ActionsRecommandees",
                column: "PrevisionEtatProduitId",
                principalTable: "PrevisionsEtatProduit",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId",
                table: "ActionsRecommandees");

            migrationBuilder.DropIndex(
                name: "IX_ActionsRecommandees_PrevisionEtatProduitId",
                table: "ActionsRecommandees");

            migrationBuilder.DropColumn(
                name: "DateFinPromotion",
                table: "Produits");

            migrationBuilder.DropColumn(
                name: "RemisePourcentage",
                table: "Produits");

            migrationBuilder.AlterColumn<int>(
                name: "PrevisionEtatProduitId",
                table: "ActionsRecommandees",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionsRecommandees_PrevisionEtatProduitId",
                table: "ActionsRecommandees",
                column: "PrevisionEtatProduitId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId",
                table: "ActionsRecommandees",
                column: "PrevisionEtatProduitId",
                principalTable: "PrevisionsEtatProduit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
