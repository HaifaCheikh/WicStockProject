using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AddGenreTailleCouleur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    UrlCible = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Lue = table.Column<bool>(type: "boolean", nullable: false),
                    RoleDestinataire = table.Column<string>(type: "text", nullable: true),
                    UtilisateurDestinataireId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TypeTissu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Categorie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Genre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Taille = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Couleur = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CycleDeVie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    RemisePourcentage = table.Column<int>(type: "integer", nullable: true),
                    DateFinPromotion = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DisponibleSurCommande = table.Column<bool>(type: "boolean", nullable: false),
                    EstArchive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Prenom = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Telephone = table.Column<string>(type: "text", nullable: true),
                    Adresse = table.Column<string>(type: "text", nullable: true),
                    CodePostal = table.Column<string>(type: "text", nullable: true),
                    Ville = table.Column<string>(type: "text", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    MotDePasseHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoriqueProductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateProduction = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    QuantiteProduite = table.Column<int>(type: "integer", nullable: false),
                    ProduitId = table.Column<int>(type: "integer", nullable: false)
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
                name: "PrevisionsEtatProduit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    TypeRisquePredit = table.Column<string>(type: "text", nullable: false),
                    ScoreRisque = table.Column<float>(type: "real", nullable: false),
                    QuantitePredite = table.Column<int>(type: "integer", nullable: false),
                    HorizonJours = table.Column<int>(type: "integer", nullable: false),
                    DateCalcul = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuantiteActuelle = table.Column<int>(type: "integer", nullable: false),
                    SeuilAlerte = table.Column<int>(type: "integer", nullable: false),
                    SeuilSurstock = table.Column<int>(type: "integer", nullable: true),
                    Emplacement = table.Column<string>(type: "text", nullable: false),
                    DateMiseAJour = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProduitId = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TypeRisque = table.Column<string>(type: "text", nullable: false),
                    DateDetection = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    NiveauCriticite = table.Column<int>(type: "integer", nullable: false),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    UtilisateurId = table.Column<int>(type: "integer", nullable: true)
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
                name: "HistoriqueVentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateVente = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    QuantiteVendue = table.Column<int>(type: "integer", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StatutCommande = table.Column<string>(type: "text", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: true),
                    DateSouhaitee = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateConfirmation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateDebutPreparation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateEstimeePreparation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DatePrete = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DatePaiement = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateLivraison = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    AdresseLivraison = table.Column<string>(type: "text", nullable: true),
                    CodePostal = table.Column<string>(type: "text", nullable: true),
                    Ville = table.Column<string>(type: "text", nullable: true),
                    Pays = table.Column<string>(type: "text", nullable: true),
                    EstSurCommande = table.Column<bool>(type: "boolean", nullable: false),
                    MontantTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstMultiLignes = table.Column<bool>(type: "boolean", nullable: false),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    Genre = table.Column<string>(type: "text", nullable: true),
                    Taille = table.Column<string>(type: "text", nullable: true),
                    Couleur = table.Column<string>(type: "text", nullable: true),
                    UtilisateurId = table.Column<int>(type: "integer", nullable: true),
                    ResponsableId = table.Column<int>(type: "integer", nullable: true),
                    LivreurId = table.Column<int>(type: "integer", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_HistoriqueVentes_Utilisateurs_LivreurId",
                        column: x => x.LivreurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoriqueVentes_Utilisateurs_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HistoriqueVentes_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActionsRecommandees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    TypeAction = table.Column<string>(type: "text", nullable: false),
                    TexteGenere = table.Column<string>(type: "text", nullable: false),
                    DateGeneration = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    PrevisionEtatProduitId = table.Column<int>(type: "integer", nullable: true),
                    UtilisateurId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionsRecommandees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProd~",
                        column: x => x.PrevisionEtatProduitId,
                        principalTable: "PrevisionsEtatProduit",
                        principalColumn: "Id");
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Motif = table.Column<string>(type: "text", nullable: false),
                    StockId = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Avis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommandeId = table.Column<int>(type: "integer", nullable: false),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<int>(type: "integer", nullable: false),
                    Commentaire = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    EstMasque = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avis_HistoriqueVentes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "HistoriqueVentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Avis_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Avis_Utilisateurs_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LigneCommandes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HistoriqueVenteId = table.Column<int>(type: "integer", nullable: false),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstSurCommande = table.Column<bool>(type: "boolean", nullable: false),
                    Genre = table.Column<string>(type: "text", nullable: true),
                    Taille = table.Column<string>(type: "text", nullable: true),
                    Couleur = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneCommandes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LigneCommandes_HistoriqueVentes_HistoriqueVenteId",
                        column: x => x.HistoriqueVenteId,
                        principalTable: "HistoriqueVentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LigneCommandes_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reclamations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommandeId = table.Column<int>(type: "integer", nullable: false),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Motif = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PhotosUrls = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    ReponseAdmin = table.Column<string>(type: "text", nullable: true),
                    DateReponse = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reclamations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reclamations_HistoriqueVentes_CommandeId",
                        column: x => x.CommandeId,
                        principalTable: "HistoriqueVentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reclamations_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reclamations_Utilisateurs_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_Avis_ClientId",
                table: "Avis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Avis_CommandeId",
                table: "Avis",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Avis_ProduitId",
                table: "Avis",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueProductions_ProduitId",
                table: "HistoriqueProductions",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueVentes_LivreurId",
                table: "HistoriqueVentes",
                column: "LivreurId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueVentes_ProduitId",
                table: "HistoriqueVentes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueVentes_ResponsableId",
                table: "HistoriqueVentes",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueVentes_UtilisateurId",
                table: "HistoriqueVentes",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_LigneCommandes_HistoriqueVenteId",
                table: "LigneCommandes",
                column: "HistoriqueVenteId");

            migrationBuilder.CreateIndex(
                name: "IX_LigneCommandes_ProduitId",
                table: "LigneCommandes",
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
                name: "IX_Reclamations_ClientId",
                table: "Reclamations",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Reclamations_CommandeId",
                table: "Reclamations",
                column: "CommandeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reclamations_ProduitId",
                table: "Reclamations",
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
                name: "Avis");

            migrationBuilder.DropTable(
                name: "HistoriqueProductions");

            migrationBuilder.DropTable(
                name: "LigneCommandes");

            migrationBuilder.DropTable(
                name: "MouvementsStock");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reclamations");

            migrationBuilder.DropTable(
                name: "PrevisionsEtatProduit");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "HistoriqueVentes");

            migrationBuilder.DropTable(
                name: "Produits");

            migrationBuilder.DropTable(
                name: "Utilisateurs");
        }
    }
}
