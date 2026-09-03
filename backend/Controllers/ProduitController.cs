using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using WicStock_.Models.Dtos;
using WicStock_.Services;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Toutes les actions nÃƒÂ©cessitent un token JWT valide
    public class ProduitController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMetriquesStockService _metriquesService;
        private readonly IAnalyseSurstockService _analyseSurstockService;
        private readonly NotificationService _notificationService;

        public ProduitController(
            AppDbContext context,
            IMetriquesStockService metriquesService,
            IAnalyseSurstockService analyseSurstockService,
            NotificationService notificationService)
        {
            _context = context;
            _metriquesService = metriquesService;
            _analyseSurstockService = analyseSurstockService;
            _notificationService = notificationService;
        }

        // GET: api/produit (Vue interne)
        [HttpGet]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<IEnumerable<Produit>>> GetProduits()
        {
            return await _context.Produits
                .Include(p => p.Stock)
                .Include(p => p.Previsions)
                    .ThenInclude(pr => pr.ActionRecommandee)
                .ToListAsync();
        }

        // GET: api/produit/catalogue (Catalogue simplifie pour CLIENT)
        [HttpGet("catalogue")]
        [Authorize(Roles = "CLIENT,ADMIN")]
        public async Task<ActionResult<IEnumerable<object>>> GetCatalogueClient()
        {
            var today = DateTime.Today;
            var produits = await _context.Produits
                .Include(p => p.Stock)
                .Where(p => !p.EstArchive)
                .ToListAsync();

            // RÃƒÂ©cupÃƒÂ©rer les actions de promotion confirmÃƒÂ©es
            var promoActions = await _context.ActionsRecommandees
                .Where(a => a.TypeAction == Enums.TypeAction.PROMOTION_CIBLEE)
                .OrderByDescending(a => a.DateGeneration)
                .ToListAsync();

            var result = new List<object>();

            foreach (var p in produits)
            {
                int remise = p.RemisePourcentage ?? 0;
                DateTime? dateFin = p.DateFinPromotion;

                // Si pas de remise enregistrÃƒÂ©e directement sur le produit, chercher dans les ActionsRecommandees (backfill)
                if (remise == 0)
                {
                    var lastPromo = promoActions.FirstOrDefault(a => a.ProduitId == p.Id);
                    if (lastPromo != null && (DateTime.Now - lastPromo.DateGeneration).TotalDays <= 30)
                    {
                        remise = 20; // valeur par dÃƒÂ©faut
                        if (!string.IsNullOrEmpty(lastPromo.TexteGenere))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(lastPromo.TexteGenere, @"Promotion de (\d+)%");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int parsedRemise))
                            {
                                remise = parsedRemise;
                            }
                        }
                        dateFin = lastPromo.DateGeneration.AddDays(14);

                        // Synchroniser les valeurs sur l'entitÃƒÂ© Produit en base
                        p.RemisePourcentage = remise;
                        p.DateFinPromotion = dateFin;
                    }
                }

                bool estEnPromo = remise > 0 && dateFin.HasValue && dateFin.Value.Date >= today;
                decimal prixPromo = estEnPromo ? Math.Round(p.PrixUnitaire * (1 - (decimal)remise / 100m), 2) : p.PrixUnitaire;
                int quantiteDisponible = p.Stock?.QuantiteActuelle ?? 0;
                int seuilAlerte = p.Stock?.SeuilAlerte ?? 10;
                bool estStockFaible = quantiteDisponible > 0 && quantiteDisponible <= seuilAlerte;
                string statutStock = quantiteDisponible <= 0
                    ? (p.DisponibleSurCommande ? "SUR_COMMANDE" : "RUPTURE")
                    : (estStockFaible ? "STOCK_FAIBLE" : "DISPONIBLE");

                result.Add(new
                {
                    p.Id,
                    p.Reference,
                    p.Nom,
                    p.TypeTissu,
                    p.Categorie,
                    p.PrixUnitaire,
                    p.ImageUrl,
                    StatutStock = statutStock,
                    RemisePourcentage = remise,
                    DateFinPromotion = dateFin,
                    EstEnPromotion = estEnPromo,
                    PrixPromo = prixPromo,
                    QuantiteDisponible = quantiteDisponible,
                    QuantiteStock = quantiteDisponible,
                    QuantiteActuelle = quantiteDisponible,
                    SeuilAlerte = seuilAlerte,
                    EstStockFaible = estStockFaible,
                    p.DisponibleSurCommande
                });
            }

            try { await _context.SaveChangesAsync(); } catch { }

            return Ok(result);
        }

        // GET: api/produit/5
        [HttpGet("{id}")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<Produit>> GetProduit(int id)
        {
            var produit = await _context.Produits
                .Include(p => p.Stock)
                .Include(p => p.Alertes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produit == null)
                return NotFound();

            return produit;
        }

        // POST: api/produit
        [HttpPost]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<Produit>> CreerProduit(Produit produit)
        {
            if (!string.IsNullOrEmpty(produit.ImageBase64))
            {
                produit.ImageUrl = SaveUploadedImage(produit.ImageBase64);
            }

            if (produit.Stock == null)
            {
                produit.Stock = new Stock
                {
                    QuantiteActuelle = 0,
                    SeuilAlerte = 10,
                    Emplacement = "Magasin principal",
                    DateMiseAJour = DateTime.Now
                };
            }
            else
            {
                produit.Stock.DateMiseAJour = DateTime.Now;
            }
            _context.Produits.Add(produit);
            await _context.SaveChangesAsync();

            if (produit.Stock != null)
            {
                var qte = produit.Stock.QuantiteActuelle > 0 ? produit.Stock.QuantiteActuelle : 1;
                _context.MouvementsStock.Add(new MouvementStock
                {
                    StockId = produit.Stock.Id,
                    Type = Enums.TypeMouvement.ENTREE,
                    Quantite = qte,
                    Date = DateTime.Now,
                    Motif = $"Ajout du produit - {produit.Nom}"
                });
                await _context.SaveChangesAsync();

                try
                {
                    if (produit.Stock.EstSousLeSeuil() && !produit.DisponibleSurCommande)
                    {
                        await _notificationService.NotifierNouvelEvenementAsync(
                            Enums.TypeNotification.RUPTURE_STOCK,
                            $"Alerte stock bas : Le produit '{produit.Nom}' est sous le seuil d'alerte ({produit.Stock.QuantiteActuelle} / {produit.Stock.SeuilAlerte} unité(s)).",
                            $"/produits/modifier/{produit.Id}",
                            Enums.RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NOTIFICATION ERROR] {ex.Message}");
                }
            }

            try
            {
                // Notifier les CLIENTS qu'un nouveau produit est disponible
                await _notificationService.NotifierNouvelEvenementAsync(
                    Enums.TypeNotification.NOUVEAU_PRODUIT,
                    $"Nouveau produit disponible ! Découvrez notre nouvel article '{produit.Nom}' dans le catalogue.",
                    $"/catalogue?produit={produit.Id}",
                    Enums.RoleUtilisateur.CLIENT
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOTIFICATION ERROR] {ex.Message}");
            }

            return CreatedAtAction(nameof(GetProduit), new { id = produit.Id }, produit);
        }

        // PUT: api/produit/5
        [HttpPut("{id}")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> ModifierProduit(int id, Produit produit)
        {
            if (id != produit.Id)
                return BadRequest();

            if (!string.IsNullOrEmpty(produit.ImageBase64))
            {
                // Supprimer l'ancienne image si elle existe
                if (!string.IsNullOrEmpty(produit.ImageUrl))
                {
                    var pathRelatif = produit.ImageUrl.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", pathRelatif);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try { System.IO.File.Delete(oldFilePath); } catch { }
                    }
                }
                produit.ImageUrl = SaveUploadedImage(produit.ImageBase64);
            }

            // Mise ÃƒÂ  jour des propriÃƒÂ©tÃƒÂ©s du produit
            var existingProduit = await _context.Produits
                .Include(p => p.Stock)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProduit == null)
                return NotFound();

            existingProduit.Reference = produit.Reference;
            existingProduit.Nom = produit.Nom;
            existingProduit.TypeTissu = produit.TypeTissu;
            existingProduit.Categorie = produit.Categorie;
            existingProduit.CycleDeVie = produit.CycleDeVie;
            existingProduit.PrixUnitaire = produit.PrixUnitaire;
            existingProduit.DisponibleSurCommande = produit.DisponibleSurCommande;
            if (!string.IsNullOrEmpty(produit.ImageUrl))
            {
                existingProduit.ImageUrl = produit.ImageUrl;
            }

            if (produit.Stock != null)
            {
                if (existingProduit.Stock != null)
                {
                    int diff = produit.Stock.QuantiteActuelle - existingProduit.Stock.QuantiteActuelle;
                    existingProduit.Stock.QuantiteActuelle = produit.Stock.QuantiteActuelle;
                    if (produit.Stock.SeuilAlerte > 0) existingProduit.Stock.SeuilAlerte = produit.Stock.SeuilAlerte;
                    if (!string.IsNullOrEmpty(produit.Stock.Emplacement)) existingProduit.Stock.Emplacement = produit.Stock.Emplacement;
                    existingProduit.Stock.DateMiseAJour = DateTime.Now;

                    if (diff != 0)
                    {
                        _context.MouvementsStock.Add(new MouvementStock
                        {
                            StockId = existingProduit.Stock.Id,
                            Type = diff > 0 ? Enums.TypeMouvement.ENTREE : Enums.TypeMouvement.SORTIE,
                            Quantite = Math.Abs(diff),
                            Date = DateTime.Now,
                            Motif = diff > 0 ? $"Ajout de stock (+{diff}) - {existingProduit.Nom}" : $"Ajustement nÃƒÂ©gatif (-{Math.Abs(diff)}) - {existingProduit.Nom}"
                        });
                    }
                }
                else
                {
                    existingProduit.Stock = new Stock
                    {
                        ProduitId = id,
                        QuantiteActuelle = produit.Stock.QuantiteActuelle,
                        SeuilAlerte = produit.Stock.SeuilAlerte > 0 ? produit.Stock.SeuilAlerte : 10,
                        Emplacement = !string.IsNullOrEmpty(produit.Stock.Emplacement) ? produit.Stock.Emplacement : "Magasin principal",
                        DateMiseAJour = DateTime.Now
                    };
                    await _context.SaveChangesAsync();

                    if (existingProduit.Stock.QuantiteActuelle > 0)
                    {
                        _context.MouvementsStock.Add(new MouvementStock
                        {
                            StockId = existingProduit.Stock.Id,
                            Type = Enums.TypeMouvement.ENTREE,
                            Quantite = existingProduit.Stock.QuantiteActuelle,
                            Date = DateTime.Now,
                            Motif = $"Initialisation stock - {existingProduit.Nom}"
                        });
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();

                if (existingProduit.Stock != null && existingProduit.Stock.EstSousLeSeuil())
                {
                    await _notificationService.NotifierNouvelEvenementAsync(
                        Enums.TypeNotification.RUPTURE_STOCK,
                        $"Alerte stock bas : Le produit '{existingProduit.Nom}' est sous le seuil d'alerte ({existingProduit.Stock.QuantiteActuelle} / {existingProduit.Stock.SeuilAlerte} unitÃƒÂ©(s)).",
                        $"/produits/modifier/{existingProduit.Id}",
                        Enums.RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION
                    );
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Produits.Any(p => p.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        private string? SaveUploadedImage(string? base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return null;

            if (base64Data.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                base64Data.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                base64Data.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return base64Data;
            }

            return $"data:image/png;base64,{base64Data}";
        }

        // DELETE: api/produit/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN,RESPONSABLE_STOCK_PRODUCTION")] // Restriction par rÃƒÂ´le
        public async Task<IActionResult> SupprimerProduit(int id)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null)
                return NotFound();

            _context.Produits.Remove(produit);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/produit/5/archiver
        [HttpPatch("{id}/archiver")]
        [Authorize(Roles = "ADMIN,RESPONSABLE_STOCK_PRODUCTION")]
        public async Task<IActionResult> ArchiverProduit(int id)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null)
                return NotFound();

            if (produit.EstArchive)
                return NoContent();

            produit.EstArchive = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/produit/5/desarchiver
        [HttpPatch("{id}/desarchiver")]
        [Authorize(Roles = "ADMIN,RESPONSABLE_STOCK_PRODUCTION")]
        public async Task<IActionResult> DesarchiverProduit(int id)
        {
            var produit = await _context.Produits.FindAsync(id);
            if (produit == null)
                return NotFound();

            if (!produit.EstArchive)
                return NoContent();

            produit.EstArchive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

                // POST: api/produit/5/analyse
        [HttpPost("{id}/analyse")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<AnalyseProduitDto>> AnalyserSurstock(int id)
        {
            try
            {
                var metriques = await _metriquesService.CalculerMetriquesProduitAsync(id);

                var produitInfo = await _context.Produits
                    .Include(p => p.Stock)
                    .FirstOrDefaultAsync(p => p.Id == id);

                metriques.NomProduit = produitInfo?.Nom ?? $"Produit #{id}";

                var analyse = await _analyseSurstockService.AnalyserSurstockAsync(metriques);

                // Vérifier si le produit a déjà une action active confirmée
                if (produitInfo != null)
                {
                    var lastConfirmedAction = await _context.ActionsRecommandees
                        .Where(a => a.ProduitId == id && a.Source == "USER_CONFIRMED_ACTION")
                        .OrderByDescending(a => a.DateGeneration)
                        .FirstOrDefaultAsync();

                    if ((produitInfo.RemisePourcentage ?? 0) > 0 && produitInfo.DateFinPromotion.HasValue && produitInfo.DateFinPromotion.Value.Date >= DateTime.Today)
                    {
                        int remise = produitInfo.RemisePourcentage.Value;
                        int dureeJours = Math.Max(1, (int)(produitInfo.DateFinPromotion.Value.Date - DateTime.Today).TotalDays);
                        decimal prixSolde = Math.Round(produitInfo.PrixUnitaire * (1 - (decimal)remise / 100m), 2);

                        int qteCible = produitInfo.Stock?.QuantiteActuelle ?? 50;
                        if (lastConfirmedAction != null && !string.IsNullOrWhiteSpace(lastConfirmedAction.TexteGenere))
                        {
                            var mQte = System.Text.RegularExpressions.Regex.Match(lastConfirmedAction.TexteGenere, @"sur (\d+) u\.");
                            if (mQte.Success && int.TryParse(mQte.Groups[1].Value, out int qc))
                            {
                                qteCible = qc;
                            }
                        }

                        analyse.ActionActive = new ActionActiveDto
                        {
                            Id = lastConfirmedAction?.Id ?? 0,
                            TypeAction = "PROMOTION_CIBLEE",
                            Label = $"Promotion ciblée (-{remise}%)",
                            Description = lastConfirmedAction?.TexteGenere ?? $"Promotion de {remise}% en cours (Prix promo : {prixSolde} DT — expire le {produitInfo.DateFinPromotion:dd/MM/yyyy})",
                            DateApplication = lastConfirmedAction?.DateGeneration ?? produitInfo.DateFinPromotion.Value.AddDays(-dureeJours),
                            DateFin = produitInfo.DateFinPromotion,
                            RemisePourcentage = remise,
                            DureeJours = dureeJours,
                            QuantiteCible = qteCible,
                            PrixPromo = prixSolde
                        };
                    }
                    else if (lastConfirmedAction != null && (DateTime.Now - lastConfirmedAction.DateGeneration).TotalDays <= 30)
                    {
                        string typeStr = lastConfirmedAction.TypeAction.ToString().ToUpperInvariant();
                        string texte = lastConfirmedAction.TexteGenere ?? "";

                        int? remiseParsee = null;
                        int? dureeParsee = null;
                        int? qteParsee = null;
                        string? motifParse = null;
                        string? destParse = null;

                        if (typeStr == "PROMOTION_CIBLEE")
                        {
                            var mRemise = System.Text.RegularExpressions.Regex.Match(texte, @"Promotion de (\d+)%");
                            var mDuree = System.Text.RegularExpressions.Regex.Match(texte, @"pendant (\d+) jour");
                            var mQte = System.Text.RegularExpressions.Regex.Match(texte, @"sur (\d+) u\.");
                            if (mRemise.Success && int.TryParse(mRemise.Groups[1].Value, out int rr)) remiseParsee = rr;
                            if (mDuree.Success && int.TryParse(mDuree.Groups[1].Value, out int dd)) dureeParsee = dd;
                            if (mQte.Success && int.TryParse(mQte.Groups[1].Value, out int qq)) qteParsee = qq;
                        }
                        else if (typeStr == "RECYCLAGE_ANTICIPE")
                        {
                            var mQte = System.Text.RegularExpressions.Regex.Match(texte, @"Recyclage confirm[eé] de (\d+)");
                            var mDest = System.Text.RegularExpressions.Regex.Match(texte, @"vers (.+?) \(Motif");
                            var mMotif = System.Text.RegularExpressions.Regex.Match(texte, @"Motif : (.+)\)");
                            if (mQte.Success && int.TryParse(mQte.Groups[1].Value, out int qq)) qteParsee = qq;
                            if (mDest.Success) destParse = mDest.Groups[1].Value.Trim();
                            if (mMotif.Success) motifParse = mMotif.Groups[1].Value.Trim();
                        }

                        analyse.ActionActive = new ActionActiveDto
                        {
                            Id = lastConfirmedAction.Id,
                            TypeAction = typeStr,
                            Label = typeStr switch
                            {
                                "PROMOTION_CIBLEE" => "Promotion ciblée",
                                "RECYCLAGE_ANTICIPE" => "Recyclage anticipé",
                                _ => "Notification production"
                            },
                            Description = texte,
                            DateApplication = lastConfirmedAction.DateGeneration,
                            RemisePourcentage = remiseParsee,
                            DureeJours = dureeParsee,
                            QuantiteCible = qteParsee,
                            Motif = motifParse,
                            Destination = destParse
                        };
                    }
                }

                return Ok(analyse);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur d'analyse : {ex.Message}" });
            }
        }

        // POST: api/produit/5/executer-action
        [HttpPost("{id}/executer-action")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> ExecuterAction(int id, [FromBody] ExecutionActionRequestDto req)
        {
            try
            {
                var produit = await _context.Produits
                    .Include(p => p.Stock)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (produit == null)
                    return NotFound(new { message = "Produit non trouvé" });

                string typeAction = req.TypeAction?.ToUpperInvariant() ?? "";
                string auditMessage = "";

                if (typeAction == "RECYCLAGE_ANTICIPE")
                {
                    int qteRecyclage = 10;
                    string motif = "Recyclage surstock persistant";
                    string destination = "Atelier interne";

                    if (req.Params != null)
                    {
                        if (req.Params.TryGetValue("quantite", out var qStr) && int.TryParse(qStr, out int q)) qteRecyclage = q;
                        if (req.Params.TryGetValue("motif", out var mStr) && !string.IsNullOrWhiteSpace(mStr)) motif = mStr;
                        if (req.Params.TryGetValue("destination", out var dStr) && !string.IsNullOrWhiteSpace(dStr)) destination = dStr;
                    }

                    if (produit.Stock != null)
                    {
                        qteRecyclage = Math.Clamp(qteRecyclage, 1, Math.Max(1, produit.Stock.QuantiteActuelle));
                        produit.Stock.QuantiteActuelle = Math.Max(0, produit.Stock.QuantiteActuelle - qteRecyclage);
                        produit.Stock.DateMiseAJour = DateTime.Now;

                        _context.MouvementsStock.Add(new MouvementStock
                        {
                            StockId = produit.Stock.Id,
                            Type = Enums.TypeMouvement.SORTIE,
                            Quantite = qteRecyclage,
                            Date = DateTime.Now,
                            Motif = $"Recyclage anticipé ({destination}) : {motif}"
                        });
                    }

                    auditMessage = $"Recyclage confirmé de {qteRecyclage} unités vers {destination} (Motif : {motif})";
                }
                else if (typeAction == "NOTIFICATION_PRODUCTION")
                {
                    string destinataire = "Chef de Production";
                    string message = "Ajustement de production requis suite à alerte surstock";
                    string priorite = "MOYENNE";

                    if (req.Params != null)
                    {
                        if (req.Params.TryGetValue("destinataire", out var dest) && !string.IsNullOrWhiteSpace(dest)) destinataire = dest;
                        if (req.Params.TryGetValue("message", out var msg) && !string.IsNullOrWhiteSpace(msg)) message = msg;
                        if (req.Params.TryGetValue("priorite", out var prio) && !string.IsNullOrWhiteSpace(prio)) priorite = prio;
                    }

                    int criticite = priorite.ToUpperInvariant() switch
                    {
                        "HAUTE" => 3,
                        "BASSE" => 1,
                        _ => 2
                    };

                    _context.Alertes.Add(new Alerte
                    {
                        ProduitId = id,
                        TypeRisque = Enums.TypeRisque.SURSTOCK,
                        DateDetection = DateTime.Now,
                        Statut = Enums.StatutAlerte.EN_COURS,
                        NiveauCriticite = criticite
                    });

                    auditMessage = $"Notification transmise à {destinataire} [Priorité {priorite}] : {message}";
                }
                else if (typeAction == "PROMOTION_CIBLEE")
                {
                    int remise = 20;
                    int duree = 14;
                    int qteCible = 50;

                    if (req.Params != null)
                    {
                        if (req.Params.TryGetValue("remisePourcentage", out var rStr) && int.TryParse(rStr, out int r)) remise = r;
                        if (req.Params.TryGetValue("dureeJours", out var dStr) && int.TryParse(dStr, out int d)) duree = d;
                        if (req.Params.TryGetValue("quantiteCible", out var qcStr) && int.TryParse(qcStr, out int qc)) qteCible = qc;
                    }

                    _context.Alertes.Add(new Alerte
                    {
                        ProduitId = id,
                        TypeRisque = Enums.TypeRisque.SURSTOCK,
                        DateDetection = DateTime.Now,
                        Statut = Enums.StatutAlerte.TRAITEE,
                        NiveauCriticite = 1
                    });

                    produit.RemisePourcentage = remise;
                    produit.DateFinPromotion = DateTime.Today.AddDays(duree);

                    decimal prixSolde = Math.Round(produit.PrixUnitaire * (1 - (decimal)remise / 100m), 2);
                    auditMessage = $"Promotion de {remise}% appliquée sur {qteCible} u. pendant {duree} jours (Prix promo : {prixSolde} DT — expire le {produit.DateFinPromotion:dd/MM/yyyy})";
                }

                // Trouver ou créer une PrevisionEtatProduit pour la contrainte FK
                var prevision = await _context.PrevisionsEtatProduit
                    .Include(p => p.ActionRecommandee)
                    .FirstOrDefaultAsync(p => p.ProduitId == id);

                if (prevision == null)
                {
                    prevision = new PrevisionEtatProduit
                    {
                        ProduitId = id,
                        TypeRisquePredit = Enums.TypeRisque.SURSTOCK,
                        ScoreRisque = 0.8f,
                        QuantitePredite = produit.Stock?.QuantiteActuelle ?? 0,
                        HorizonJours = 30,
                        DateCalcul = DateTime.Now
                    };
                    _context.PrevisionsEtatProduit.Add(prevision);
                    await _context.SaveChangesAsync();
                }

                // Supprimer les anciennes actions recommandées liées à ce produit ou à cette prévision
                var anciennesActions = await _context.ActionsRecommandees
                    .Where(a => a.ProduitId == id || a.PrevisionEtatProduitId == prevision.Id)
                    .ToListAsync();

                if (anciennesActions.Any())
                {
                    _context.ActionsRecommandees.RemoveRange(anciennesActions);
                    await _context.SaveChangesAsync();
                }

                // Insérer la nouvelle action confirmée unique
                _context.ActionsRecommandees.Add(new ActionRecommandee
                {
                    ProduitId = id,
                    PrevisionEtatProduitId = prevision.Id,
                    TypeAction = typeAction switch
                    {
                        "RECYCLAGE_ANTICIPE" => Enums.TypeAction.RECYCLAGE_ANTICIPE,
                        "PROMOTION_CIBLEE" => Enums.TypeAction.PROMOTION_CIBLEE,
                        _ => Enums.TypeAction.REDISTRIBUTION
                    },
                    TexteGenere = string.IsNullOrWhiteSpace(auditMessage) ? $"Exécution {req.ActionLabel}" : auditMessage,
                    DateGeneration = DateTime.Now,
                    Source = "USER_CONFIRMED_ACTION"
                });

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = string.IsNullOrWhiteSpace(auditMessage) ? $"Action '{req.ActionLabel}' exécutée avec succès." : auditMessage,
                    nouveauStock = produit.Stock?.QuantiteActuelle ?? 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur d'exécution de l'action : {ex.Message}", detail = ex.InnerException?.Message });
            }
        }

        // POST: api/produit/5/annuler-action
        [HttpPost("{id}/annuler-action")]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<IActionResult> AnnulerAction(int id)
        {
            try
            {
                var produit = await _context.Produits.FindAsync(id);
                if (produit == null) return NotFound(new { message = "Produit non trouvé" });

                // 1. Réinitialiser la promotion sur le produit
                produit.RemisePourcentage = null;
                produit.DateFinPromotion = null;

                // 2. Supprimer les actions confirmées associées à ce produit
                var actionsConfirmees = await _context.ActionsRecommandees
                    .Where(a => a.ProduitId == id && a.Source == "USER_CONFIRMED_ACTION")
                    .ToListAsync();

                if (actionsConfirmees.Any())
                {
                    _context.ActionsRecommandees.RemoveRange(actionsConfirmees);
                }

                // 3. Nettoyer les alertes liées si applicable
                var alertes = await _context.Alertes
                    .Where(a => a.ProduitId == id && a.TypeRisque == Enums.TypeRisque.SURSTOCK && a.Statut == Enums.StatutAlerte.TRAITEE)
                    .ToListAsync();
                foreach (var al in alertes)
                {
                    al.Statut = Enums.StatutAlerte.NON_TRAITEE;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Action annulée avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de l'annulation : {ex.Message}" });
            }
        }
    }
}