using Microsoft.EntityFrameworkCore;

/// <summary>
/// Applies idempotent SQL Server schema patches when the model has evolved without EF migrations.
/// </summary>
public static class DatabaseSchemaBootstrap
{
    public static async Task ApplyAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH('Produits', 'RemisePourcentage') IS NULL
                    ALTER TABLE Produits ADD RemisePourcentage int NULL;

                IF COL_LENGTH('Produits', 'DateFinPromotion') IS NULL
                    ALTER TABLE Produits ADD DateFinPromotion datetime2 NULL;

                IF COL_LENGTH('Produits', 'DisponibleSurCommande') IS NULL
                    ALTER TABLE Produits ADD DisponibleSurCommande bit NOT NULL CONSTRAINT DF_Produits_DisponibleSurCommande DEFAULT 0;

                IF COL_LENGTH('Produits', 'EstArchive') IS NULL
                    ALTER TABLE Produits ADD EstArchive bit NOT NULL CONSTRAINT DF_Produits_EstArchive DEFAULT 0;

                IF COL_LENGTH('HistoriqueVentes', 'UtilisateurId') IS NULL
                    ALTER TABLE HistoriqueVentes ADD UtilisateurId int NULL;

                IF COL_LENGTH('HistoriqueVentes', 'DateSouhaitee') IS NULL
                    ALTER TABLE HistoriqueVentes ADD DateSouhaitee datetime2 NULL;

                IF COL_LENGTH('HistoriqueVentes', 'AdresseLivraison') IS NULL
                    ALTER TABLE HistoriqueVentes ADD AdresseLivraison nvarchar(500) NULL;

                IF COL_LENGTH('HistoriqueVentes', 'CodePostal') IS NULL
                    ALTER TABLE HistoriqueVentes ADD CodePostal nvarchar(50) NULL;

                IF COL_LENGTH('HistoriqueVentes', 'Ville') IS NULL
                    ALTER TABLE HistoriqueVentes ADD Ville nvarchar(100) NULL;

                IF COL_LENGTH('HistoriqueVentes', 'Pays') IS NULL
                    ALTER TABLE HistoriqueVentes ADD Pays nvarchar(100) NULL;

                IF COL_LENGTH('Utilisateurs', 'Adresse') IS NULL
                    ALTER TABLE Utilisateurs ADD Adresse nvarchar(500) NULL;

                IF COL_LENGTH('Utilisateurs', 'CodePostal') IS NULL
                    ALTER TABLE Utilisateurs ADD CodePostal nvarchar(50) NULL;

                IF COL_LENGTH('Utilisateurs', 'Ville') IS NULL
                    ALTER TABLE Utilisateurs ADD Ville nvarchar(100) NULL;

                IF COL_LENGTH('HistoriqueVentes', 'DateEstimeePreparation') IS NULL
                    ALTER TABLE HistoriqueVentes ADD DateEstimeePreparation datetime2 NULL;

                IF COL_LENGTH('HistoriqueVentes', 'Statut') IS NULL
                    ALTER TABLE HistoriqueVentes ADD Statut nvarchar(64) NULL;

                IF COL_LENGTH('HistoriqueVentes', 'DateConfirmation') IS NULL
                    ALTER TABLE HistoriqueVentes ADD DateConfirmation datetime2 NULL;

                IF COL_LENGTH('HistoriqueVentes', 'DateDebutPreparation') IS NULL
                    ALTER TABLE HistoriqueVentes ADD DateDebutPreparation datetime2 NULL;

                IF COL_LENGTH('HistoriqueVentes', 'DatePrete') IS NULL
                    ALTER TABLE HistoriqueVentes ADD DatePrete datetime2 NULL;

                IF COL_LENGTH('HistoriqueVentes', 'EstSurCommande') IS NULL
                    ALTER TABLE HistoriqueVentes ADD EstSurCommande bit NOT NULL CONSTRAINT DF_HistoriqueVentes_EstSurCommande DEFAULT 0;

                IF COL_LENGTH('HistoriqueVentes', 'ResponsableId') IS NULL
                    ALTER TABLE HistoriqueVentes ADD ResponsableId int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HistoriqueVentes_Responsable')
                    ALTER TABLE HistoriqueVentes ADD CONSTRAINT FK_HistoriqueVentes_Responsable FOREIGN KEY (ResponsableId) REFERENCES Utilisateurs(Id) ON DELETE NO ACTION;

                IF COL_LENGTH('HistoriqueVentes', 'DatePaiement') IS NULL
                    ALTER TABLE HistoriqueVentes ADD DatePaiement datetime2 NULL;

                IF COL_LENGTH('HistoriqueVentes', 'DateLivraison') IS NULL
                    ALTER TABLE HistoriqueVentes ADD DateLivraison datetime2 NULL;

                IF COL_LENGTH('HistoriqueVentes', 'PaymentIntentId') IS NULL
                    ALTER TABLE HistoriqueVentes ADD PaymentIntentId nvarchar(max) NULL;

                IF COL_LENGTH('HistoriqueVentes', 'LivreurId') IS NULL
                    ALTER TABLE HistoriqueVentes ADD LivreurId int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HistoriqueVentes_Livreur')
                    ALTER TABLE HistoriqueVentes ADD CONSTRAINT FK_HistoriqueVentes_Livreur FOREIGN KEY (LivreurId) REFERENCES Utilisateurs(Id) ON DELETE NO ACTION;

                IF COL_LENGTH('Notifications', 'UtilisateurDestinataireId') IS NULL
                    ALTER TABLE Notifications ADD UtilisateurDestinataireId int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Notifications')
                BEGIN
                    CREATE TABLE Notifications (
                        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Type nvarchar(64) NOT NULL,
                        Message nvarchar(max) NOT NULL,
                        UrlCible nvarchar(500) NULL,
                        DateCreation datetime2 NOT NULL,
                        Lue bit NOT NULL CONSTRAINT DF_Notifications_Lue DEFAULT 0,
                        RoleDestinataire nvarchar(64) NULL
                    );
                END

                UPDATE Produits
                SET DateCreation = COALESCE(
                    (SELECT MIN(d) FROM (
                        SELECT MIN(m.Date) AS d FROM Stocks s JOIN MouvementsStock m ON s.Id = m.StockId WHERE s.ProduitId = Produits.Id
                        UNION ALL
                        SELECT MIN(v.DateVente) AS d FROM HistoriqueVentes v WHERE v.ProduitId = Produits.Id
                    ) AS dates WHERE d IS NOT NULL),
                    CAST('2026-07-20 10:00:00' AS DATETIME2)
                )
                WHERE DateCreation < '2020-01-01';
                """);

            logger.LogInformation("Database schema bootstrap completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database schema bootstrap skipped or partially failed.");
        }
    }
}
