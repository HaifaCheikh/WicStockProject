using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WicStock_.Migrations
{
    /// <inheritdoc />
    public partial class AddVarianteProduitEtAttributs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VarianteProduitId",
                table: "LigneCommandes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttributsValeurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Valeur = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodeHex = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributsValeurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariantesProduit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProduitId = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Genre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Taille = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Couleur = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QuantiteActuelle = table.Column<int>(type: "integer", nullable: false),
                    SeuilAlerte = table.Column<int>(type: "integer", nullable: false),
                    PrixOverride = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantesProduit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantesProduit_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LigneCommandes_VarianteProduitId",
                table: "LigneCommandes",
                column: "VarianteProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributsValeurs_Type_Valeur",
                table: "AttributsValeurs",
                columns: new[] { "Type", "Valeur" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantesProduit_ProduitId_Genre_Taille_Couleur",
                table: "VariantesProduit",
                columns: new[] { "ProduitId", "Genre", "Taille", "Couleur" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantesProduit_Reference",
                table: "VariantesProduit",
                column: "Reference",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LigneCommandes_VariantesProduit_VarianteProduitId",
                table: "LigneCommandes",
                column: "VarianteProduitId",
                principalTable: "VariantesProduit",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LigneCommandes_VariantesProduit_VarianteProduitId",
                table: "LigneCommandes");

            migrationBuilder.DropTable(
                name: "AttributsValeurs");

            migrationBuilder.DropTable(
                name: "VariantesProduit");

            migrationBuilder.DropIndex(
                name: "IX_LigneCommandes_VarianteProduitId",
                table: "LigneCommandes");

            migrationBuilder.DropColumn(
                name: "VarianteProduitId",
                table: "LigneCommandes");
        }
    }
}
