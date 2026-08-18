using System.Net.Http.Json;
using WicStock.Web.Models.Dtos;

namespace WicStock.Web.Services
{
    /// <summary>
    /// Service singleton qui maintient le compteur de commandes EN_ATTENTE
    /// pour afficher un badge rouge dans la sidebar (Admin + Responsable Stock).
    /// </summary>
    public class CommandeBadgeService
    {
        private readonly HttpClient _http;
        private readonly AuthService _authService;

        public int CommandesEnAttente { get; private set; } = 0;

        /// <summary>Déclenché à chaque fois que le compteur change.</summary>
        public event Action? OnCountChanged;

        public CommandeBadgeService(HttpClient http, AuthService authService)
        {
            _http = http;
            _authService = authService;
        }

        public async Task RafraichirAsync()
        {
            try
            {
                var commandes = await _http.GetFromJsonAsync<List<CommandeManagerDto>>("api/HistoriqueVente")
                                ?? new List<CommandeManagerDto>();

                var userRole = await _authService.ObtenirRole();
                var nouveau = 0;

                if (userRole == "ADMIN")
                {
                    // Admin: compte les commandes sur commande en attente de confirmation (EN_ATTENTE_CONFIRMATION)
                    // et les commandes standards en attente (EN_ATTENTE)
                    nouveau = commandes.Count(c => 
                        (c.EstSurCommande && (c.Statut ?? "") == "EN_ATTENTE_CONFIRMATION") ||
                        (!c.EstSurCommande && c.StatutCommande == "EN_ATTENTE"));
                }
                else if (userRole == "RESPONSABLE_STOCK_PRODUCTION")
                {
                    // Responsable: compte les commandes qui lui sont assignées et en attente de son action
                    // Pour l'instant, on compte toutes les commandes sur commande confirmées ou en préparation
                    // Dans une implémentation complète, il faudrait filtrer par ResponsableId == currentUserId
                    nouveau = commandes.Count(c => 
                        c.EstSurCommande && 
                        c.ResponsableId.HasValue && 
                        ((c.Statut ?? "") == "CONFIRMEE" || (c.Statut ?? "") == "EN_PREPARATION"));
                }
                else
                {
                    // Pour les autres rôles (CLIENT), on ne compte rien
                    nouveau = 0;
                }

                if (nouveau != CommandesEnAttente)
                {
                    CommandesEnAttente = nouveau;
                    OnCountChanged?.Invoke();
                }
            }
            catch
            {
                // Silencieux si l'API n'est pas joignable (ex: déconnecté)
            }
        }
    }
}
