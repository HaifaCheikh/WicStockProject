using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using WicStock_.Models.Dtos;
using static WicStock_.Models.Enums;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<AdminDashboardDto>> GetAdminDashboard()
        {
            var totalProduits = await _context.Produits.CountAsync();
            var totalUsers = await _context.Utilisateurs.CountAsync();
            var totalCommandes = await _context.HistoriqueVentes.CountAsync();

            // Revenus : uniquement les commandes rÃ©ellement payÃ©es (statut enum)
            var ventesValidees = await _context.HistoriqueVentes
                .Include(v => v.Produit)
                .Where(v => v.Statut == StatutCommandeDetaille.PAYEE
                         || v.Statut == StatutCommandeDetaille.EN_LIVRAISON
                         || v.Statut == StatutCommandeDetaille.LIVREE)
                .ToListAsync();

            var totalProduitsVendus = ventesValidees.Sum(v => v.QuantiteVendue);
            var revenuTotal = ventesValidees.Sum(v => v.QuantiteVendue * v.PrixUnitaire);

            var produitsEnRupture = await _context.Stocks
                .Where(s => s.QuantiteActuelle == 0)
                .CountAsync();

            // GÃ©nÃ©rer toujours 6 mois complets (du 5Ã¨me mois passÃ© au mois en cours)
            var now = DateTime.Now;
            var evolution = new List<MonthlyRevenueDto>();

            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                // Pour l'Ã©volution mensuelle : on utilise DatePaiement si disponible, sinon DateVente
                var totalMois = ventesValidees
                    .Where(v => (v.DatePaiement ?? v.DateVente) >= monthStart && (v.DatePaiement ?? v.DateVente) < monthEnd)
                    .Sum(v => v.QuantiteVendue * v.PrixUnitaire);

                string monthLabel = targetMonth.ToString("MMM yy");
                evolution.Add(new MonthlyRevenueDto
                {
                    Mois = monthLabel,
                    Montant = totalMois
                });
            }

            // Top 5 produits les plus vendus
            var topProduits = ventesValidees
                .GroupBy(v => v.Produit != null ? v.Produit.Nom : "Article")
                .Select(g => new TopProduitDto
                {
                    Nom = g.Key,
                    QuantiteVendue = g.Sum(v => v.QuantiteVendue),
                    TotalVentes = g.Sum(v => v.QuantiteVendue * v.PrixUnitaire)
                })
                .OrderByDescending(t => t.QuantiteVendue)
                .Take(5)
                .ToList();

            var statuts = new StatutCommandesDto
            {
                Acceptees = await _context.HistoriqueVentes.CountAsync(v =>
                    v.StatutCommande == "ACCEPTEE"
                    || v.Statut == StatutCommandeDetaille.CONFIRMEE
                    || v.Statut == StatutCommandeDetaille.PAYEE
                    || v.Statut == StatutCommandeDetaille.EN_LIVRAISON
                    || v.Statut == StatutCommandeDetaille.LIVREE),
                EnAttente = await _context.HistoriqueVentes.CountAsync(v =>
                    v.StatutCommande == "EN_ATTENTE"
                    || v.Statut == StatutCommandeDetaille.EN_ATTENTE_CONFIRMATION
                    || v.Statut == StatutCommandeDetaille.EN_PREPARATION
                    || v.Statut == StatutCommandeDetaille.PRETE),
                Refusees = await _context.HistoriqueVentes.CountAsync(v =>
                    v.StatutCommande == "REFUSEE"
                    || v.Statut == StatutCommandeDetaille.REFUSEE)
            };

            var produitsSurstock = await _context.Stocks
                .Include(s => s.Produit)
                .Where(s => s.QuantiteActuelle >= 100)
                .Select(s => new WicStock_.Models.Dtos.ProduitSurstockDto
                {
                    Id = s.ProduitId,
                    Nom = s.Produit != null ? s.Produit.Nom : "Article",
                    Reference = s.Produit != null ? s.Produit.Reference : "",
                    QuantiteActuelle = s.QuantiteActuelle,
                    PourcentageSurstock = s.QuantiteActuelle > 100 ? (int)Math.Round(((double)s.QuantiteActuelle - 100) / 100 * 100) : 0
                })
                .OrderByDescending(p => p.PourcentageSurstock)
                .Take(5)
                .ToListAsync();

            var dto = new AdminDashboardDto
            {
                TotalProduits = totalProduits,
                TotalProduitsVendus = totalProduitsVendus,
                RevenuTotal = revenuTotal,
                TotalUtilisateurs = totalUsers,
                TotalCommandes = totalCommandes,
                ProduitsEnRupture = produitsEnRupture,
                EvolutionRevenus = evolution,
                TopProduitsVendus = topProduits,
                StatutCommandes = statuts,
                ProduitsEnSurstock = produitsSurstock
            };

            return Ok(dto);
        }

        [HttpGet("manager")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<ManagerDashboardDto>> GetManagerDashboard()
        {
            var totalStockArticles = await _context.Stocks.SumAsync(s => (int?)s.QuantiteActuelle) ?? 0;

            var produitsAlerteList = await _context.Stocks
                .Include(s => s.Produit)
                .Where(s => s.QuantiteActuelle <= s.SeuilAlerte)
                .Select(s => new ProduitStockAlerteDto
                {
                    Id = s.ProduitId,
                    Nom = s.Produit != null ? s.Produit.Nom : "Article",
                    Reference = s.Produit != null ? s.Produit.Reference : "",
                    QuantiteActuelle = s.QuantiteActuelle,
                    SeuilAlerte = s.SeuilAlerte
                })
                .ToListAsync();

            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

            // Revenus du manager : uniquement les commandes payÃ©es (statut enum)
            var ventesAujourdhuiList = await _context.HistoriqueVentes
                .Where(v => (v.Statut == StatutCommandeDetaille.PAYEE
                          || v.Statut == StatutCommandeDetaille.EN_LIVRAISON
                          || v.Statut == StatutCommandeDetaille.LIVREE)
                         && (v.DatePaiement ?? v.DateVente) >= today)
                .ToListAsync();
            var ventesAujourdhui = ventesAujourdhuiList.Sum(v => v.QuantiteVendue * v.PrixUnitaire);
            var produitsVendusAujourdhui = ventesAujourdhuiList.Sum(v => v.QuantiteVendue);

            var ventesSemaineList = await _context.HistoriqueVentes
                .Where(v => (v.Statut == StatutCommandeDetaille.PAYEE
                          || v.Statut == StatutCommandeDetaille.EN_LIVRAISON
                          || v.Statut == StatutCommandeDetaille.LIVREE)
                         && (v.DatePaiement ?? v.DateVente) >= startOfWeek)
                .ToListAsync();
            var ventesSemaine = ventesSemaineList.Sum(v => v.QuantiteVendue * v.PrixUnitaire);

            var commandesEnAttenteCount = await _context.HistoriqueVentes
                .CountAsync(v => v.StatutCommande == "EN_ATTENTE");

            var mouvementsRecents = await _context.MouvementsStock
                .Include(m => m.Stock)
                    .ThenInclude(s => s!.Produit)
                .OrderByDescending(m => m.Date)
                .Take(10)
                .Select(m => new MouvementRecentDto
                {
                    Id = m.Id,
                    ProduitNom = m.Stock != null && m.Stock.Produit != null ? m.Stock.Produit.Nom : "Article",
                    TypeMouvement = m.Type.ToString(),
                    Quantite = m.Quantite,
                    DateMouvement = m.Date
                })
                .ToListAsync();

            var produitsSurstock = await _context.Stocks
                .Include(s => s.Produit)
                .Where(s => s.QuantiteActuelle >= 100)
                .Select(s => new WicStock_.Models.Dtos.ProduitSurstockDto
                {
                    Id = s.ProduitId,
                    Nom = s.Produit != null ? s.Produit.Nom : "Article",
                    Reference = s.Produit != null ? s.Produit.Reference : "",
                    QuantiteActuelle = s.QuantiteActuelle,
                    PourcentageSurstock = s.QuantiteActuelle > 100 ? (int)Math.Round(((double)s.QuantiteActuelle - 100) / 100 * 100) : 0
                })
                .OrderByDescending(p => p.PourcentageSurstock)
                .Take(5)
                .ToListAsync();

            var ventesSimple = ventesAujourdhuiList.Select(v => new { v.Id, v.QuantiteVendue, v.DateVente }).ToList();

            var dto = new ManagerDashboardDto
            {
                TotalStockArticles = totalStockArticles,
                ProduitsStockFaible = produitsAlerteList.Count,
                TotalProduitsVendus = produitsVendusAujourdhui,
                RevenuAujourdhui = ventesAujourdhui,
                RevenuSemaine = ventesSemaine,
                CommandesEnAttenteCount = commandesEnAttenteCount,
                ProduitsAlerte = produitsAlerteList,
                MouvementsRecents = mouvementsRecents,
                ProduitsEnSurstock = produitsSurstock
            };

            return Ok(dto);
        }
    }
}
