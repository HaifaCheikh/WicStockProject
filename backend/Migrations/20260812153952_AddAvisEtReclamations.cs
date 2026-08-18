using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AddAvisEtReclamations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Avis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandeId = table.Column<int>(type: "int", nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstMasque = table.Column<bool>(type: "bit", nullable: false)
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
                name: "Reclamations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandeId = table.Column<int>(type: "int", nullable: false),
                    ProduitId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotosUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReponseAdmin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateReponse = table.Column<DateTime>(type: "datetime2", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Avis");

            migrationBuilder.DropTable(
                name: "Reclamations");
        }
    }
}
