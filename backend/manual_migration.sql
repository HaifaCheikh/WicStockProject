IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [Produits] (
        [Id] int NOT NULL IDENTITY,
        [Reference] nvarchar(50) NOT NULL,
        [Nom] nvarchar(150) NOT NULL,
        [TypeTissu] nvarchar(100) NOT NULL,
        [Categorie] nvarchar(100) NOT NULL,
        [CycleDeVie] nvarchar(100) NOT NULL,
        [DateCreation] datetime2 NOT NULL,
        CONSTRAINT [PK_Produits] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [Utilisateurs] (
        [Id] int NOT NULL IDENTITY,
        [Nom] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [MotDePasseHash] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Utilisateurs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [HistoriqueProductions] (
        [Id] int NOT NULL IDENTITY,
        [DateProduction] datetime2 NOT NULL,
        [QuantiteProduite] int NOT NULL,
        [ProduitId] int NOT NULL,
        CONSTRAINT [PK_HistoriqueProductions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoriqueProductions_Produits_ProduitId] FOREIGN KEY ([ProduitId]) REFERENCES [Produits] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [HistoriqueVentes] (
        [Id] int NOT NULL IDENTITY,
        [DateVente] datetime2 NOT NULL,
        [QuantiteVendue] int NOT NULL,
        [PrixUnitaire] decimal(18,2) NOT NULL,
        [ProduitId] int NOT NULL,
        CONSTRAINT [PK_HistoriqueVentes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoriqueVentes_Produits_ProduitId] FOREIGN KEY ([ProduitId]) REFERENCES [Produits] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [PrevisionsEtatProduit] (
        [Id] int NOT NULL IDENTITY,
        [ProduitId] int NOT NULL,
        [TypeRisquePredit] nvarchar(max) NOT NULL,
        [ScoreRisque] real NOT NULL,
        [QuantitePredite] int NOT NULL,
        [HorizonJours] int NOT NULL,
        [DateCalcul] datetime2 NOT NULL,
        CONSTRAINT [PK_PrevisionsEtatProduit] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrevisionsEtatProduit_Produits_ProduitId] FOREIGN KEY ([ProduitId]) REFERENCES [Produits] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [Stocks] (
        [Id] int NOT NULL IDENTITY,
        [QuantiteActuelle] int NOT NULL,
        [SeuilAlerte] int NOT NULL,
        [Emplacement] nvarchar(max) NOT NULL,
        [DateMiseAJour] datetime2 NOT NULL,
        [ProduitId] int NOT NULL,
        CONSTRAINT [PK_Stocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stocks_Produits_ProduitId] FOREIGN KEY ([ProduitId]) REFERENCES [Produits] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [Alertes] (
        [Id] int NOT NULL IDENTITY,
        [TypeRisque] nvarchar(max) NOT NULL,
        [DateDetection] datetime2 NOT NULL,
        [Statut] nvarchar(max) NOT NULL,
        [NiveauCriticite] int NOT NULL,
        [ProduitId] int NOT NULL,
        [UtilisateurId] int NULL,
        CONSTRAINT [PK_Alertes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Alertes_Produits_ProduitId] FOREIGN KEY ([ProduitId]) REFERENCES [Produits] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Alertes_Utilisateurs_UtilisateurId] FOREIGN KEY ([UtilisateurId]) REFERENCES [Utilisateurs] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [ActionsRecommandees] (
        [Id] int NOT NULL IDENTITY,
        [ProduitId] int NOT NULL,
        [TypeAction] nvarchar(max) NOT NULL,
        [TexteGenere] nvarchar(max) NOT NULL,
        [DateGeneration] datetime2 NOT NULL,
        [Source] nvarchar(max) NOT NULL,
        [PrevisionEtatProduitId] int NOT NULL,
        [UtilisateurId] int NULL,
        CONSTRAINT [PK_ActionsRecommandees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId] FOREIGN KEY ([PrevisionEtatProduitId]) REFERENCES [PrevisionsEtatProduit] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ActionsRecommandees_Utilisateurs_UtilisateurId] FOREIGN KEY ([UtilisateurId]) REFERENCES [Utilisateurs] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE TABLE [MouvementsStock] (
        [Id] int NOT NULL IDENTITY,
        [Type] nvarchar(max) NOT NULL,
        [Quantite] int NOT NULL,
        [Date] datetime2 NOT NULL,
        [Motif] nvarchar(max) NOT NULL,
        [StockId] int NOT NULL,
        CONSTRAINT [PK_MouvementsStock] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MouvementsStock_Stocks_StockId] FOREIGN KEY ([StockId]) REFERENCES [Stocks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ActionsRecommandees_PrevisionEtatProduitId] ON [ActionsRecommandees] ([PrevisionEtatProduitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActionsRecommandees_UtilisateurId] ON [ActionsRecommandees] ([UtilisateurId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alertes_ProduitId] ON [Alertes] ([ProduitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alertes_UtilisateurId] ON [Alertes] ([UtilisateurId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HistoriqueProductions_ProduitId] ON [HistoriqueProductions] ([ProduitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HistoriqueVentes_ProduitId] ON [HistoriqueVentes] ([ProduitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MouvementsStock_StockId] ON [MouvementsStock] ([StockId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PrevisionsEtatProduit_ProduitId] ON [PrevisionsEtatProduit] ([ProduitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Stocks_ProduitId] ON [Stocks] ([ProduitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719002226_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719002226_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719005138_AjoutPrenom'
)
BEGIN
    ALTER TABLE [Utilisateurs] ADD [Prenom] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719005138_AjoutPrenom'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719005138_AjoutPrenom', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719013742_miseajour'
)
BEGIN
    ALTER TABLE [Utilisateurs] ADD [Telephone] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719013742_miseajour'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719013742_miseajour', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719174430_AddImageUrlToProduit'
)
BEGIN
    ALTER TABLE [Produits] ADD [ImageUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719174430_AddImageUrlToProduit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719174430_AddImageUrlToProduit', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723002545_AjoutUtilisateurCommande'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [UtilisateurId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723002545_AjoutUtilisateurCommande'
)
BEGIN
    CREATE INDEX [IX_HistoriqueVentes_UtilisateurId] ON [HistoriqueVentes] ([UtilisateurId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723002545_AjoutUtilisateurCommande'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD CONSTRAINT [FK_HistoriqueVentes_Utilisateurs_UtilisateurId] FOREIGN KEY ([UtilisateurId]) REFERENCES [Utilisateurs] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723002545_AjoutUtilisateurCommande'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723002545_AjoutUtilisateurCommande', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723025757_AjoutStatutCommande'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [StatutCommande] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723025757_AjoutStatutCommande'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723025757_AjoutStatutCommande', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723041650_AjoutPrixUnitaireProduit'
)
BEGIN
    ALTER TABLE [Produits] ADD [PrixUnitaire] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723041650_AjoutPrixUnitaireProduit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723041650_AjoutPrixUnitaireProduit', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803224519_AjoutPhotoUrlUtilisateur'
)
BEGIN
    ALTER TABLE [Utilisateurs] ADD [PhotoUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803224519_AjoutPhotoUrlUtilisateur'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803224519_AjoutPhotoUrlUtilisateur', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    ALTER TABLE [ActionsRecommandees] DROP CONSTRAINT [FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    DROP INDEX [IX_ActionsRecommandees_PrevisionEtatProduitId] ON [ActionsRecommandees];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    ALTER TABLE [Produits] ADD [DateFinPromotion] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    ALTER TABLE [Produits] ADD [RemisePourcentage] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ActionsRecommandees]') AND [c].[name] = N'PrevisionEtatProduitId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ActionsRecommandees] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [ActionsRecommandees] ALTER COLUMN [PrevisionEtatProduitId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ActionsRecommandees_PrevisionEtatProduitId] ON [ActionsRecommandees] ([PrevisionEtatProduitId]) WHERE [PrevisionEtatProduitId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    ALTER TABLE [ActionsRecommandees] ADD CONSTRAINT [FK_ActionsRecommandees_PrevisionsEtatProduit_PrevisionEtatProduitId] FOREIGN KEY ([PrevisionEtatProduitId]) REFERENCES [PrevisionsEtatProduit] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260805214307_AjoutPromotionProduit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260805214307_AjoutPromotionProduit', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808135007_AddNotificationTable'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [Type] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [UrlCible] nvarchar(max) NULL,
        [DateCreation] datetime2 NOT NULL,
        [Lue] bit NOT NULL,
        [RoleDestinataire] nvarchar(max) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260808135007_AddNotificationTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260808135007_AddNotificationTable', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [Produits] ADD [DisponibleSurCommande] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [Notifications] ADD [UtilisateurDestinataireId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [DateConfirmation] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [DateDebutPreparation] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [DateEstimeePreparation] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [DateLivraison] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [DatePaiement] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [DatePrete] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [DateSouhaitee] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [EstSurCommande] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [PaymentIntentId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [ResponsableId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD [Statut] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    CREATE INDEX [IX_HistoriqueVentes_ResponsableId] ON [HistoriqueVentes] ([ResponsableId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    ALTER TABLE [HistoriqueVentes] ADD CONSTRAINT [FK_HistoriqueVentes_Utilisateurs_ResponsableId] FOREIGN KEY ([ResponsableId]) REFERENCES [Utilisateurs] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810014359_AddPaymentFieldsToHistoriqueVente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810014359_AddPaymentFieldsToHistoriqueVente', N'8.0.10');
END;
GO

COMMIT;
GO

