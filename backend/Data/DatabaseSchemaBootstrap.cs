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
                """);

            logger.LogInformation("Database schema bootstrap (PostgreSQL) completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database schema bootstrap skipped or partially failed: {Message}", ex.Message);
        }
    }
}
