using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WicStock_.Models;
using static WicStock_.Models.Enums;

namespace WicStock_.Services
{
    public class CommandeExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandeExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
        public const int ExpirationDaysDefault = 7;

        public CommandeExpirationService(
            IServiceProvider serviceProvider,
            ILogger<CommandeExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[CommandeExpirationService] Service démarré. Vérification toutes les 1 heure des commandes non payées.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AnnulerCommandesExpireesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CommandeExpirationService] Erreur lors du nettoyage des commandes expirées.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        public async Task<int> AnnulerCommandesExpireesAsync(int expirationDays = ExpirationDaysDefault)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

            var limiteDate = DateTime.Now.AddDays(-expirationDays);

            // Rechercher toutes les commandes créées il y a plus de N jours sans paiement
            var commandesExpirees = await context.HistoriqueVentes
                .Include(h => h.Produit)
                    .ThenInclude(p => p.Stock)
                .Include(h => h.Produit)
                    .ThenInclude(p => p.Variantes)
                .Include(h => h.LigneCommandes)
                    .ThenInclude(l => l.VarianteProduit)
                .Where(h => h.DatePaiement == null
                            && h.DateVente <= limiteDate
                            && h.StatutCommande != "PAYEE"
                            && h.StatutCommande != "LIVREE"
                            && h.StatutCommande != "REFUSEE"
                            && h.StatutCommande != "ANNULEE"
                            && h.Statut != StatutCommandeDetaille.PAYEE
                            && h.Statut != StatutCommandeDetaille.EN_LIVRAISON
                            && h.Statut != StatutCommandeDetaille.LIVREE
                            && h.Statut != StatutCommandeDetaille.REFUSEE
                            && h.Statut != StatutCommandeDetaille.ANNULEE)
                .ToListAsync();

            if (!commandesExpirees.Any())
            {
                return 0;
            }

            int nbAnnulees = 0;
            foreach (var commande in commandesExpirees)
            {
                _logger.LogInformation("[CommandeExpirationService] Annulation automatique de la commande #{CommandeId} (non payée depuis le {DateVente:dd/MM/yyyy}).",
                    commande.Id, commande.DateVente);

                // 1. Restituer le stock au catalogue et aux variantes
                await RestituerStockCommandeAsync(context, commande);

                // 2. Marquer la commande comme ANNULEE
                commande.StatutCommande = "ANNULEE";
                commande.Statut = StatutCommandeDetaille.ANNULEE;

                // 3. Notifier le client
                if (commande.UtilisateurId.HasValue)
                {
                    await notificationService.NotifierNouvelEvenementAsync(
                        TypeNotification.COMMANDE_EN_ATTENTE,
                        $"Votre commande #{commande.Id} du {commande.DateVente:dd/MM/yyyy} a été annulée automatiquement car le paiement n'a pas été effectué dans le délai de {expirationDays} jours. Les articles ont été remis en stock.",
                        $"/mes-commandes/suivi/{commande.Id}",
                        RoleUtilisateur.CLIENT,
                        commande.UtilisateurId.Value
                    );
                }

                // 4. Notifier le responsable du stock
                await notificationService.NotifierNouvelEvenementAsync(
                    TypeNotification.COMMANDE_EN_ATTENTE,
                    $"Commande #{commande.Id} annulée automatiquement pour défaut de paiement ({expirationDays} jours). Stock restitué.",
                    "/commandes",
                    RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION
                );

                nbAnnulees++;
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("[CommandeExpirationService] {Count} commande(s) non payée(s) expirée(s) annulée(s) avec succès.", nbAnnulees);

            return nbAnnulees;
        }

        public static async Task RestituerStockCommandeAsync(AppDbContext context, HistoriqueVente vente)
        {
            if (vente == null) return;

            if (vente.StatutCommande == "REFUSEE" || vente.StatutCommande == "ANNULEE" ||
                vente.Statut == StatutCommandeDetaille.REFUSEE || vente.Statut == StatutCommandeDetaille.ANNULEE)
                return;

            if (vente.EstMultiLignes && vente.LigneCommandes != null && vente.LigneCommandes.Any())
            {
                foreach (var ligne in vente.LigneCommandes.Where(l => !l.EstSurCommande))
                {
                    if (ligne.VarianteProduitId.HasValue && ligne.VarianteProduitId.Value > 0)
                    {
                        var variante = ligne.VarianteProduit ?? await context.VariantesProduit.FindAsync(ligne.VarianteProduitId.Value);
                        if (variante != null)
                        {
                            variante.QuantiteActuelle += ligne.Quantite;
                        }
                    }

                    var stock = await context.Stocks.FirstOrDefaultAsync(s => s.ProduitId == ligne.ProduitId);
                    if (stock != null)
                    {
                        stock.QuantiteActuelle += ligne.Quantite;
                        stock.DateMiseAJour = DateTime.Now;

                        context.MouvementsStock.Add(new MouvementStock
                        {
                            StockId = stock.Id,
                            Type = TypeMouvement.ENTREE,
                            Quantite = ligne.Quantite,
                            Date = DateTime.Now,
                            Motif = $"Restitution suite annulation automatique (défaut de paiement) commande #{vente.Id}"
                        });
                    }
                }
            }
            else if (!vente.EstSurCommande)
            {
                if (!string.IsNullOrWhiteSpace(vente.Genre) || !string.IsNullOrWhiteSpace(vente.Taille) || !string.IsNullOrWhiteSpace(vente.Couleur))
                {
                    var variante = await context.VariantesProduit.FirstOrDefaultAsync(v => v.ProduitId == vente.ProduitId &&
                        (string.IsNullOrWhiteSpace(vente.Genre) || v.Genre == vente.Genre) &&
                        (string.IsNullOrWhiteSpace(vente.Taille) || v.Taille == vente.Taille) &&
                        (string.IsNullOrWhiteSpace(vente.Couleur) || v.Couleur == vente.Couleur));
                    if (variante != null)
                    {
                        variante.QuantiteActuelle += vente.QuantiteVendue;
                    }
                }

                var stock = await context.Stocks.FirstOrDefaultAsync(s => s.ProduitId == vente.ProduitId);
                if (stock != null)
                {
                    stock.QuantiteActuelle += vente.QuantiteVendue;
                    stock.DateMiseAJour = DateTime.Now;

                    context.MouvementsStock.Add(new MouvementStock
                    {
                        StockId = stock.Id,
                        Type = TypeMouvement.ENTREE,
                        Quantite = vente.QuantiteVendue,
                        Date = DateTime.Now,
                        Motif = $"Restitution suite annulation automatique (défaut de paiement) commande #{vente.Id}"
                    });
                }
            }
        }
    }
}
