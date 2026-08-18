using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using WicStock_.Models.Dtos;

namespace WicStock_.Services
{
    public interface IMetriquesStockService
    {
        Task<MetriquesStockDto> CalculerMetriquesProduitAsync(int produitId);
    }

    public class MetriquesStockService : IMetriquesStockService
    {
        private readonly AppDbContext _context;

        public MetriquesStockService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MetriquesStockDto> CalculerMetriquesProduitAsync(int produitId)
        {
            var produit = await _context.Produits
                .Include(p => p.Stock)
                    .ThenInclude(s => s!.Mouvements)
                .Include(p => p.HistoriqueVentes)
                .FirstOrDefaultAsync(p => p.Id == produitId);

            if (produit == null)
            {
                throw new ArgumentException($"Produit non trouvé (ID: {produitId})");
            }

            int stockActuel = produit.Stock?.QuantiteActuelle ?? 0;
            
            // Le seuil de surstock métier WicStock est fixé à 100 unités
            int seuilSurstock = (produit.Stock?.SeuilAlerte > 50) ? produit.Stock.SeuilAlerte : 100;
            
            // Pourcentage au-dessus du seuil de surstock
            double pourcentageAuDessus = 0;
            if (stockActuel > seuilSurstock)
            {
                pourcentageAuDessus = Math.Round(((double)(stockActuel - seuilSurstock) / seuilSurstock) * 100.0, 1);
            }

            // Date d'ajout : utilise DateCreation si valide, sinon la première activité connue
            var dateCreationReelle = produit.DateCreation.Year > 2000 ? produit.DateCreation : (DateTime?)null;

            // Première vente ou premier mouvement enregistré (pour les anciens produits sans DateCreation)
            var premiereSortie = produit.Stock?.Mouvements?
                .Where(m => m.Type == Enums.TypeMouvement.SORTIE)
                .OrderBy(m => m.Date)
                .FirstOrDefault()?.Date;

            var premiereVente = produit.HistoriqueVentes?
                .OrderBy(v => v.DateVente)
                .FirstOrDefault()?.DateVente;

            DateTime? datePremierActivite = null;
            if (premiereSortie.HasValue && premiereVente.HasValue)
                datePremierActivite = premiereSortie < premiereVente ? premiereSortie : premiereVente;
            else
                datePremierActivite = premiereSortie ?? premiereVente;

            var dateEffectiveAjout = dateCreationReelle ?? datePremierActivite;
            string dateAjoutFormatee = dateEffectiveAjout.HasValue
                ? dateEffectiveAjout.Value.ToString("dd/MM/yyyy")
                : "Non renseignée";

            // Calcul inactivité : dernière vente ou sortie de stock (la plus récente)
            var derniereSortie = produit.Stock?.Mouvements?
                .Where(m => m.Type == Enums.TypeMouvement.SORTIE)
                .OrderByDescending(m => m.Date)
                .FirstOrDefault()?.Date;

            var derniereVente = produit.HistoriqueVentes?
                .OrderByDescending(v => v.DateVente)
                .FirstOrDefault()?.DateVente;

            DateTime? derniereAction = null;
            if (derniereSortie.HasValue && derniereVente.HasValue)
                derniereAction = derniereSortie > derniereVente ? derniereSortie : derniereVente;
            else
                derniereAction = derniereSortie ?? derniereVente;

            int joursDepuisDerniereSortie;
            if (derniereAction.HasValue && derniereAction.Value.Year > 2000)
                joursDepuisDerniereSortie = (int)Math.Max(0, (DateTime.Now - derniereAction.Value).TotalDays);
            else if (dateEffectiveAjout.HasValue)
                joursDepuisDerniereSortie = (int)Math.Max(0, (DateTime.Now - dateEffectiveAjout.Value).TotalDays);
            else
                joursDepuisDerniereSortie = 0;

            // Taux d'écoulement du produit sur 90 jours
            var ilYA90Jours = DateTime.Now.AddDays(-90);
            int totalSorties90 = produit.Stock?.Mouvements?
                .Where(m => m.Type == Enums.TypeMouvement.SORTIE && m.Date >= ilYA90Jours)
                .Sum(m => m.Quantite) ?? 0;
                
            double tauxEcoulement90Jours = Math.Round(((double)totalSorties90 / Math.Max(stockActuel + totalSorties90, 1)) * 100.0, 1);

            // Métriques comparatives de la catégorie
            string cat = string.IsNullOrWhiteSpace(produit.Categorie) ? "Générale" : produit.Categorie;

            var produitsCategorie = await _context.Produits
                .Include(p => p.Stock)
                .Where(p => p.Id != produitId && p.Categorie == cat)
                .ToListAsync();

            int totalProdsCat = produitsCategorie.Count;
            int nbProdsSurstockCat = produitsCategorie
                .Count(p => (p.Stock?.QuantiteActuelle ?? 0) > ((p.Stock?.SeuilAlerte > 50) ? p.Stock.SeuilAlerte : 100));

            bool estTendanceCategorie = totalProdsCat > 0 && (nbProdsSurstockCat >= 2 || ((double)nbProdsSurstockCat / totalProdsCat) >= 0.3);

            double tauxEcoulementMoyenCategorie = 45.0;
            int dureeEcoulementSimilaires = 14;

            // Valeur immobilisée & coût possession mensuel (3%)
            decimal valeurStock = (decimal)stockActuel * (produit.PrixUnitaire > 0 ? produit.PrixUnitaire : 100m);
            decimal coutPossessionMensuel = Math.Round(valeurStock * 0.03m, 2);

            return new MetriquesStockDto
            {
                ProduitId = produit.Id,
                NomProduit = produit.Nom,
                StockActuel = stockActuel,
                SeuilSurstock = seuilSurstock,
                PourcentageAuDessusDuSeuil = pourcentageAuDessus,
                JoursDepuisDerniereSortie = joursDepuisDerniereSortie,
                DateAjoutFormatee = dateAjoutFormatee,
                TauxEcoulement90Jours = tauxEcoulement90Jours,
                Categorie = cat,
                TauxEcoulementMoyenCategorie90Jours = tauxEcoulementMoyenCategorie,
                EstTendanceCategorie = estTendanceCategorie,
                NbReferencesSimilairesEnSurstock = nbProdsSurstockCat,
                DureeEcoulementMoyenneProduitsSimilaires = dureeEcoulementSimilaires,
                ValeurStockImmobilisee = valeurStock,
                CoutPossessionEstimeMensuel = coutPossessionMensuel
            };
        }
    }
}
