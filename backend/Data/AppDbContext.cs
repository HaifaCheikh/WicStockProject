using Microsoft.EntityFrameworkCore;
using WicStock_.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produit> Produits { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<MouvementStock> MouvementsStock { get; set; }
    public DbSet<HistoriqueVente> HistoriqueVentes { get; set; }
    public DbSet<HistoriqueProduction> HistoriqueProductions { get; set; }
    public DbSet<Alerte> Alertes { get; set; }
    public DbSet<Utilisateur> Utilisateurs { get; set; }
    public DbSet<PrevisionEtatProduit> PrevisionsEtatProduit { get; set; }
    public DbSet<ActionRecommandee> ActionsRecommandees { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Avis> Avis { get; set; }
    public DbSet<Reclamation> Reclamations { get; set; }
    public DbSet<LigneCommande> LigneCommandes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Produit <-> Stock : relation 1-1 stricte
        modelBuilder.Entity<Produit>()
            .HasOne(p => p.Stock)
            .WithOne(s => s.Produit)
            .HasForeignKey<Stock>(s => s.ProduitId);

        // Précision des décimales pour éviter les troncatures
        modelBuilder.Entity<Produit>()
            .Property(p => p.PrixUnitaire)
            .HasPrecision(18, 2);

        // PrevisionEtatProduit <-> ActionRecommandee : relation 1 - 0..1
        modelBuilder.Entity<PrevisionEtatProduit>()
            .HasOne(p => p.ActionRecommandee)
            .WithOne(a => a.PrevisionEtatProduit)
            .HasForeignKey<ActionRecommandee>(a => a.PrevisionEtatProduitId);

        // Stocker les enums en texte plutôt qu'en nombre (plus lisible en base)
        modelBuilder.Entity<MouvementStock>()
            .Property(m => m.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Alerte>()
            .Property(a => a.TypeRisque)
            .HasConversion<string>();

        modelBuilder.Entity<Alerte>()
            .Property(a => a.Statut)
            .HasConversion<string>();

        modelBuilder.Entity<Utilisateur>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<PrevisionEtatProduit>()
            .Property(p => p.TypeRisquePredit)
            .HasConversion<string>();

        modelBuilder.Entity<ActionRecommandee>()
            .Property(a => a.TypeAction)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.RoleDestinataire)
            .HasConversion<string>();

        modelBuilder.Entity<Avis>()
            .Property(a => a.Statut)
            .HasConversion<string>();

        modelBuilder.Entity<Reclamation>()
            .Property(r => r.Statut)
            .HasConversion<string>();

        // Éviter les suppressions en cascade multiples (SQL Server les refuse par défaut)
        modelBuilder.Entity<Alerte>()
            .HasOne(a => a.Utilisateur)
            .WithMany(u => u.AlertesTraitees)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActionRecommandee>()
            .HasOne(a => a.Utilisateur)
            .WithMany(u => u.ActionsValidees)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HistoriqueVente>()
            .HasOne(h => h.Utilisateur)
            .WithMany()
            .HasForeignKey(h => h.UtilisateurId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HistoriqueVente>()
            .HasOne(h => h.Responsable)
            .WithMany()
            .HasForeignKey(h => h.ResponsableId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<HistoriqueVente>()
            .Property(h => h.Statut)
            .HasConversion<string>();

        // Précision des décimales pour HistoriqueVente
        modelBuilder.Entity<HistoriqueVente>()
            .Property(h => h.PrixUnitaire)
            .HasPrecision(18, 2);

        modelBuilder.Entity<HistoriqueVente>()
            .Property(h => h.MontantTotal)
            .HasPrecision(18, 2);

        // LigneCommande — relation avec HistoriqueVente (cascade delete : supprimer la commande supprime ses lignes)
        modelBuilder.Entity<LigneCommande>()
            .HasOne(l => l.HistoriqueVente)
            .WithMany(h => h.LigneCommandes)
            .HasForeignKey(l => l.HistoriqueVenteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LigneCommande>()
            .HasOne(l => l.Produit)
            .WithMany()
            .HasForeignKey(l => l.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LigneCommande>()
            .Property(l => l.PrixUnitaire)
            .HasPrecision(18, 2);

        // Configuration Avis
        modelBuilder.Entity<Avis>()
            .HasOne(a => a.Commande)
            .WithMany()
            .HasForeignKey(a => a.CommandeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Avis>()
            .HasOne(a => a.Produit)
            .WithMany()
            .HasForeignKey(a => a.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Avis>()
            .HasOne(a => a.Client)
            .WithMany()
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuration Reclamation
        modelBuilder.Entity<Reclamation>()
            .HasOne(r => r.Commande)
            .WithMany()
            .HasForeignKey(r => r.CommandeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reclamation>()
            .HasOne(r => r.Produit)
            .WithMany()
            .HasForeignKey(r => r.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reclamation>()
            .HasOne(r => r.Client)
            .WithMany()
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}