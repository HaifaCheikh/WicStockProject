using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Produits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TypeTissu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CycleDeVie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotDePasseHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoriqueProductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateProduction = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantiteProduite = table.Column<int>(type: "int", nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriqueProductions_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistoriqueVentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateVente = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantiteVendue = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueVentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriqueVentes_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrevisionsEtatProduit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProduitId = table.Column<int>(type: "int", nullable: false),
                    TypeRisquePredit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoreRisque = table.Column<float>(type: "real", nullable: false),
                    QuantitePredite = table.Column<int>(type: "int", nullable: false),
                    HorizonJours = table.Column<int>(type: "int", nullable: false),
                    DateCalcul = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrevisionsEtatProduit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrevisionsEtatProduit_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuantiteActuelle = table.Column<int>(type: "int", nullable: false),
                    SeuilAlerte = table.Column<int>(type: "int", nullable: false),
                    Emplacement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateMiseAJour = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocks_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alertes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeRisque = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateDetection = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NiveauCriticite = table.Column<int>(type: "int", nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alertes_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alertes_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActionsRecommandees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProduitId = table.Column<int>(type: "int", nullable: false),
                    TypeAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TexteGenere = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateGeneration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrevisionEtatProduitId = table.Column<int>(type: "int", nullable: false),
                    UtilisateurId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionsRecommandees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId",
                        column: x => x.PrevisionEtatProduitId,
                        principalTable: "PrevisionsEtatProduit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActionsRecommandees_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MouvementsStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MouvementsStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MouvementsStock_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionsRecommandees_PrevisionEtatProduitId",
                table: "ActionsRecommandees",
                column: "PrevisionEtatProduitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionsRecommandees_UtilisateurId",
                table: "ActionsRecommandees",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertes_ProduitId",
                table: "Alertes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertes_UtilisateurId",
                table: "Alertes",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueProductions_ProduitId",
                table: "HistoriqueProductions",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueVentes_ProduitId",
                table: "HistoriqueVentes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_StockId",
                table: "MouvementsStock",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_PrevisionsEtatProduit_ProduitId",
                table: "PrevisionsEtatProduit",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProduitId",
                table: "Stocks",
                column: "ProduitId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionsRecommandees");

            migrationBuilder.DropTable(
                name: "Alertes");

            migrationBuilder.DropTable(
                name: "HistoriqueProductions");

            migrationBuilder.DropTable(
                name: "HistoriqueVentes");

            migrationBuilder.DropTable(
                name: "MouvementsStock");

            migrationBuilder.DropTable(
                name: "PrevisionsEtatProduit");

            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "Produits");
        }
    }
}
