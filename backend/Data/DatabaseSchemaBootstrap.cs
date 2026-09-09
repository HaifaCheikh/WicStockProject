using Microsoft.EntityFrameworkCore;

/// <summary>
/// Applies idempotent PostgreSQL schema patches when the model has evolved without EF migrations.
/// Compatible with Aiven / Supabase PostgreSQL.
/// </summary>
public static class DatabaseSchemaBootstrap
{
    public static async Task ApplyAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            // Ensure EF Core creates all tables first (EnsureCreated for PostgreSQL)
            await context.Database.EnsureCreatedAsync();

            // Idempotent column additions (PostgreSQL syntax)
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "Produits" ADD COLUMN IF NOT EXISTS "RemisePourcentage" int NULL;
                ALTER TABLE "Produits" ADD COLUMN IF NOT EXISTS "DateFinPromotion" timestamp NULL;
                ALTER TABLE "Produits" ADD COLUMN IF NOT EXISTS "DisponibleSurCommande" boolean NOT NULL DEFAULT false;
                ALTER TABLE "Produits" ADD COLUMN IF NOT EXISTS "EstArchive" boolean NOT NULL DEFAULT false;
                ALTER TABLE "Produits" ADD COLUMN IF NOT EXISTS "Genre" varchar(50) NULL;
                ALTER TABLE "Produits" ADD COLUMN IF NOT EXISTS "Taille" varchar(50) NULL;
                ALTER TABLE "Produits" ADD COLUMN IF NOT EXISTS "Couleur" varchar(50) NULL;

                ALTER TABLE "LigneCommandes" ADD COLUMN IF NOT EXISTS "Genre" varchar(50) NULL;
                ALTER TABLE "LigneCommandes" ADD COLUMN IF NOT EXISTS "Taille" varchar(50) NULL;
                ALTER TABLE "LigneCommandes" ADD COLUMN IF NOT EXISTS "Couleur" varchar(50) NULL;

                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "Genre" varchar(50) NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "Taille" varchar(50) NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "Couleur" varchar(50) NULL;

                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "UtilisateurId" int NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "DateSouhaitee" timestamp NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "AdresseLivraison" varchar(500) NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "CodePostal" varchar(50) NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "Ville" varchar(100) NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "Pays" varchar(100) NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "DateEstimeePreparation" timestamp NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "Statut" varchar(64) NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "DateConfirmation" timestamp NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "DateDebutPreparation" timestamp NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "DatePrete" timestamp NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "EstSurCommande" boolean NOT NULL DEFAULT false;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "ResponsableId" int NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "DatePaiement" timestamp NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "DateLivraison" timestamp NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "PaymentIntentId" text NULL;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "LivreurId" int NULL;

                ALTER TABLE "Utilisateurs" ADD COLUMN IF NOT EXISTS "Adresse" varchar(500) NULL;
                ALTER TABLE "Utilisateurs" ADD COLUMN IF NOT EXISTS "CodePostal" varchar(50) NULL;
                ALTER TABLE "Utilisateurs" ADD COLUMN IF NOT EXISTS "Ville" varchar(100) NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_name = 'Notifications'
                    ) THEN
                        CREATE TABLE "Notifications" (
                            "Id" SERIAL PRIMARY KEY,
                            "Type" varchar(64) NOT NULL,
                            "Message" text NOT NULL,
                            "UrlCible" varchar(500) NULL,
                            "DateCreation" timestamp NOT NULL,
                            "Lue" boolean NOT NULL DEFAULT false,
                            "RoleDestinataire" varchar(64) NULL
                        );
                    END IF;
                END $$;

                ALTER TABLE "Notifications" ADD COLUMN IF NOT EXISTS "UtilisateurDestinataireId" int NULL;

                -- ===== Commandes multi-articles (v2) =====
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "MontantTotal" numeric(18,2) NOT NULL DEFAULT 0;
                ALTER TABLE "HistoriqueVentes" ADD COLUMN IF NOT EXISTS "EstMultiLignes" boolean NOT NULL DEFAULT false;

                -- Table LigneCommandes (créée si absente — idempotent)
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_name = 'LigneCommandes'
                    ) THEN
                        CREATE TABLE "LigneCommandes" (
                            "Id" SERIAL PRIMARY KEY,
                            "HistoriqueVenteId" int NOT NULL,
                            "ProduitId" int NOT NULL,
                            "Quantite" int NOT NULL,
                            "PrixUnitaire" numeric(18,2) NOT NULL,
                            "EstSurCommande" boolean NOT NULL DEFAULT false,
                            CONSTRAINT "FK_LigneCommandes_HistoriqueVentes" FOREIGN KEY ("HistoriqueVenteId")
                                REFERENCES "HistoriqueVentes"("Id") ON DELETE CASCADE,
                            CONSTRAINT "FK_LigneCommandes_Produits" FOREIGN KEY ("ProduitId")
                                REFERENCES "Produits"("Id") ON DELETE RESTRICT
                        );
                        CREATE INDEX "IX_LigneCommandes_HistoriqueVenteId" ON "LigneCommandes"("HistoriqueVenteId");
                        CREATE INDEX "IX_LigneCommandes_ProduitId" ON "LigneCommandes"("ProduitId");
                    END IF;
                END $$;
                -- ===== AttributsValeurs (Genres, Tailles, Couleurs Denim) =====
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_name = 'AttributsValeurs'
                    ) THEN
                        CREATE TABLE "AttributsValeurs" (
                            "Id" SERIAL PRIMARY KEY,
                            "Type" varchar(30) NOT NULL,
                            "Valeur" varchar(100) NOT NULL,
                            "CodeHex" varchar(10) NULL,
                            "Ordre" int NOT NULL DEFAULT 0,
                            "Actif" boolean NOT NULL DEFAULT true
                        );
                        CREATE UNIQUE INDEX "IX_AttributsValeurs_Type_Valeur" ON "AttributsValeurs"("Type", "Valeur");
                    END IF;
                END $$;

                -- ===== VariantesProduit =====
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_name = 'VariantesProduit'
                    ) THEN
                        CREATE TABLE "VariantesProduit" (
                            "Id" SERIAL PRIMARY KEY,
                            "ProduitId" int NOT NULL,
                            "Reference" varchar(100) NOT NULL,
                            "Genre" varchar(50) NULL,
                            "Taille" varchar(50) NULL,
                            "Couleur" varchar(50) NULL,
                            "QuantiteActuelle" int NOT NULL DEFAULT 0,
                            "SeuilAlerte" int NOT NULL DEFAULT 10,
                            "PrixOverride" numeric(18,2) NULL,
                            CONSTRAINT "FK_VariantesProduit_Produits" FOREIGN KEY ("ProduitId")
                                REFERENCES "Produits"("Id") ON DELETE CASCADE
                        );
                        CREATE UNIQUE INDEX "IX_VariantesProduit_Reference" ON "VariantesProduit"("Reference");
                        CREATE UNIQUE INDEX "IX_VariantesProduit_Produit_Genre_Taille_Couleur" ON "VariantesProduit"("ProduitId", "Genre", "Taille", "Couleur");
                    END IF;
                END $$;

                ALTER TABLE "LigneCommandes" ADD COLUMN IF NOT EXISTS "VarianteProduitId" int NULL;
                """);

            // ===== SEEDING INITIAL DES ATTRIBUTS =====
            if (!await context.AttributsValeurs.AnyAsync())
            {
                var seedAttributs = new List<WicStock_.Models.AttributValeur>
                {
                    // Genres
                    new() { Type = "Genre", Valeur = "Homme", Ordre = 1 },
                    new() { Type = "Genre", Valeur = "Femme", Ordre = 2 },
                    new() { Type = "Genre", Valeur = "Enfant", Ordre = 3 },
                    new() { Type = "Genre", Valeur = "Unisex", Ordre = 4 },

                    // Tailles Vêtements
                    new() { Type = "Taille", Valeur = "XS", Ordre = 1 },
                    new() { Type = "Taille", Valeur = "S", Ordre = 2 },
                    new() { Type = "Taille", Valeur = "M", Ordre = 3 },
                    new() { Type = "Taille", Valeur = "L", Ordre = 4 },
                    new() { Type = "Taille", Valeur = "XL", Ordre = 5 },
                    new() { Type = "Taille", Valeur = "XXL", Ordre = 6 },

                    // Tailles Numériques / Pointures
                    new() { Type = "Taille", Valeur = "36", Ordre = 7 },
                    new() { Type = "Taille", Valeur = "38", Ordre = 8 },
                    new() { Type = "Taille", Valeur = "40", Ordre = 9 },
                    new() { Type = "Taille", Valeur = "42", Ordre = 10 },
                    new() { Type = "Taille", Valeur = "44", Ordre = 11 },
                    new() { Type = "Taille", Valeur = "Standard", Ordre = 12 },

                    // Palette Denim
                    new() { Type = "Couleur", Valeur = "Bleu clair délavé", CodeHex = "#7B9CC4", Ordre = 1 },
                    new() { Type = "Couleur", Valeur = "Bleu indigo", CodeHex = "#3B5488", Ordre = 2 },
                    new() { Type = "Couleur", Valeur = "Bleu moyen", CodeHex = "#4A6FA5", Ordre = 3 },
                    new() { Type = "Couleur", Valeur = "Bleu foncé / brut", CodeHex = "#1F3A5F", Ordre = 4 },
                    new() { Type = "Couleur", Valeur = "Noir denim", CodeHex = "#1C1C1E", Ordre = 5 },
                    new() { Type = "Couleur", Valeur = "Gris denim", CodeHex = "#6E7075", Ordre = 6 },
                    new() { Type = "Couleur", Valeur = "Blanc cassé", CodeHex = "#F0EBE1", Ordre = 7 },
                    new() { Type = "Couleur", Valeur = "Beige / sable", CodeHex = "#C9BBA0", Ordre = 8 },
                    new() { Type = "Couleur", Valeur = "Marron / tabac", CodeHex = "#6B4A2F", Ordre = 9 }
                };

                await context.AttributsValeurs.AddRangeAsync(seedAttributs);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeding initial des attributs (Genres, Tailles, Couleurs Denim) réussi.");
            }

            // ===== MIGRATION AUTOMATIQUE DES PRODUITS EXISTANTS VERS DÉCLINAISONS =====
            var produitsSansVariantes = await context.Produits
                .Include(p => p.Stock)
                .Include(p => p.Variantes)
                .Where(p => !p.Variantes.Any())
                .ToListAsync();

            if (produitsSansVariantes.Any())
            {
                foreach (var prod in produitsSansVariantes)
                {
                    var qty = prod.Stock?.QuantiteActuelle ?? 0;
                    var seuil = prod.Stock?.SeuilAlerte ?? 10;
                    var sku = $"{prod.Reference}-STD";

                    context.VariantesProduit.Add(new WicStock_.Models.VarianteProduit
                    {
                        ProduitId = prod.Id,
                        Reference = sku,
                        Genre = prod.Genre ?? "Unisex",
                        Taille = prod.Taille ?? "Standard",
                        Couleur = prod.Couleur ?? "Bleu indigo",
                        QuantiteActuelle = qty,
                        SeuilAlerte = seuil
                    });
                }
                await context.SaveChangesAsync();
                logger.LogInformation("Migration automatique de {Count} produits existants vers 1 variante par défaut effectuée.", produitsSansVariantes.Count);
            }

            logger.LogInformation("Database schema bootstrap (PostgreSQL) completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database schema bootstrap skipped or partially failed: {Message}", ex.Message);
        }
    }
}
