using System.Text.Json;
using Microsoft.JSInterop;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    /// <summary>
    /// Service de gestion du panier multi-articles.
    /// Persisté en localStorage (clé "wicstock_cart") — survit aux rechargements de page.
    /// Fonctionne sans authentification (panier disponible avant connexion).
    /// </summary>
    public class CartService
    {
        private readonly LocalStorageService _localStorage;
        private const string StorageKey = "wicstock_cart";

        private List<CartItemDto> _items = new();
        private bool _loaded = false;

        /// <summary>
        /// Événement déclenché à chaque modification du panier.
        /// S'abonner pour mettre à jour le badge navbar ou rafraîchir l'UI.
        /// </summary>
        public event Action? OnCartChanged;

        public CartService(LocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // ─────────────────────────────────────────────
        // Accès aux données
        // ─────────────────────────────────────────────

        /// <summary>Retourne les articles du panier (charge depuis localStorage si nécessaire).</summary>
        public async Task<List<CartItemDto>> GetItemsAsync()
        {
            await EnsureLoadedAsync();
            return _items;
        }

        /// <summary>Nombre total d'articles (somme des quantités).</summary>
        public async Task<int> GetCountAsync()
        {
            await EnsureLoadedAsync();
            return _items.Sum(i => i.Quantite);
        }

        /// <summary>Nombre d'articles (synchrone — utiliser après OnCartChanged).</summary>
        public int Count => _items.Sum(i => i.Quantite);

        /// <summary>Total du panier en euros (affichage — re-validé côté backend à la validation).</summary>
        public decimal GetTotal() => _items.Sum(i => i.SousTotal);

        // ─────────────────────────────────────────────
        // Mutations
        // ─────────────────────────────────────────────

        /// <summary>
        /// Ajoute un produit au panier ou incrémente sa quantité s'il y est déjà.
        /// Respecte la quantité disponible (sauf pour les produits sur commande).
        /// </summary>
        public async Task AddToCartAsync(CatalogueProduitDto produit, int quantite = 1)
        {
            await EnsureLoadedAsync();

            var existant = _items.FirstOrDefault(i => i.ProduitId == produit.Id);
            if (existant != null)
            {
                // Incrémente — respecte la limite de stock si non commandable
                int maxQty = produit.DisponibleSurCommande
                    ? existant.Quantite + quantite  // pas de limite pour les commandes sur commande
                    : Math.Min(existant.Quantite + quantite, produit.QuantiteDisponible);
                existant.Quantite = maxQty;
            }
            else
            {
                int qty = produit.DisponibleSurCommande
                    ? quantite
                    : Math.Min(quantite, produit.QuantiteDisponible);

                _items.Add(new CartItemDto
                {
                    ProduitId = produit.Id,
                    Nom = produit.Nom,
                    Reference = produit.Reference,
                    ImageUrl = produit.ImageUrl,
                    Categorie = produit.Categorie,
                    TypeTissu = produit.TypeTissu,
                    PrixUnitaire = produit.PrixUnitaire,
                    PrixEffectif = produit.PrixEffectif,
                    EstEnPromotion = produit.EstEnPromotion,
                    RemisePourcentage = produit.RemisePourcentage,
                    Quantite = Math.Max(1, qty),
                    QuantiteDisponible = produit.QuantiteDisponible,
                    DisponibleSurCommande = produit.DisponibleSurCommande
                });
            }

            await PersisterAsync();
            OnCartChanged?.Invoke();
        }

        /// <summary>Supprime un article du panier.</summary>
        public async Task RemoveFromCartAsync(int produitId)
        {
            await EnsureLoadedAsync();
            _items.RemoveAll(i => i.ProduitId == produitId);
            await PersisterAsync();
            OnCartChanged?.Invoke();
        }

        /// <summary>Met à jour la quantité d'un article. Si quantite <= 0, supprime l'article.</summary>
        public async Task UpdateQuantityAsync(int produitId, int nouvelleQuantite)
        {
            await EnsureLoadedAsync();
            if (nouvelleQuantite <= 0)
            {
                await RemoveFromCartAsync(produitId);
                return;
            }

            var item = _items.FirstOrDefault(i => i.ProduitId == produitId);
            if (item != null)
            {
                // Respecte la limite de stock sauf si commandable
                item.Quantite = item.DisponibleSurCommande
                    ? nouvelleQuantite
                    : Math.Min(nouvelleQuantite, item.QuantiteDisponible > 0 ? item.QuantiteDisponible : nouvelleQuantite);
                await PersisterAsync();
                OnCartChanged?.Invoke();
            }
        }

        /// <summary>Vide complètement le panier.</summary>
        public async Task ClearCartAsync()
        {
            _items.Clear();
            await PersisterAsync();
            OnCartChanged?.Invoke();
        }

        /// <summary>
        /// Applique les corrections de stock après un 409 du backend.
        /// Met à jour les quantités des articles concernés sans vider le reste du panier.
        /// </summary>
        public async Task AppliquerCorrectionsStockAsync(List<LigneStockErrorClientDto> erreurs)
        {
            await EnsureLoadedAsync();
            foreach (var erreur in erreurs)
            {
                var item = _items.FirstOrDefault(i => i.ProduitId == erreur.ProduitId);
                if (item != null)
                {
                    if (erreur.QuantiteDisponible <= 0)
                    {
                        _items.Remove(item);
                    }
                    else
                    {
                        item.Quantite = erreur.QuantiteDisponible;
                        item.QuantiteDisponible = erreur.QuantiteDisponible;
                    }
                }
            }
            await PersisterAsync();
            OnCartChanged?.Invoke();
        }

        // ─────────────────────────────────────────────
        // Persistance localStorage
        // ─────────────────────────────────────────────

        private async Task EnsureLoadedAsync()
        {
            if (_loaded) return;
            try
            {
                var json = await _localStorage.GetItemAsync(StorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    _items = JsonSerializer.Deserialize<List<CartItemDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<CartItemDto>();
                }
            }
            catch
            {
                _items = new List<CartItemDto>();
            }
            _loaded = true;
        }

        private async Task PersisterAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_items);
                await _localStorage.SetItemAsync(StorageKey, json);
            }
            catch { /* Silencieux si localStorage indisponible */ }
        }

        /// <summary>Force le rechargement depuis localStorage (utile après un changement externe).</summary>
        public void ResetCache() => _loaded = false;
    }
}
