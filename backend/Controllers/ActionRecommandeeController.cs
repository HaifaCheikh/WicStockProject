using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using WicStock_.Services;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
    public class ActionRecommandeeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAExplicationService _iaService;

        public ActionRecommandeeController(AppDbContext context, IAExplicationService iaService)
        {
            _context = context;
            _iaService = iaService;
        }

        // GET: api/actionrecommandee
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActionRecommandee>>> GetActions()
        {
            return await _context.ActionsRecommandees
                .Include(a => a.PrevisionEtatProduit)
                .Include(a => a.Utilisateur)
                .OrderByDescending(a => a.DateGeneration)
                .ToListAsync();
        }

        // GET: api/actionrecommandee/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ActionRecommandee>> GetAction(int id)
        {
            var action = await _context.ActionsRecommandees
                .Include(a => a.PrevisionEtatProduit)
                .Include(a => a.Utilisateur)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (action == null)
                return NotFound();

            return action;
        }

        // POST: api/actionrecommandee
        // Endpoint appelé par FastAPI une fois la recommandation générée par Qwen3
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<ActionRecommandee>> CreerAction(ActionRecommandee action)
        {
            if (string.IsNullOrWhiteSpace(action.TexteGenere))
            {
                var prevision = await _context.PrevisionsEtatProduit
                    .Include(p => p.Produit)
                    .ThenInclude(p => p!.Stock)
                    .FirstOrDefaultAsync(p => p.Id == action.PrevisionEtatProduitId);

                if (prevision != null && prevision.Produit != null && prevision.Produit.Stock != null)
                {
                    var explication = await _iaService.GenererExplication(
                        nomProduit: prevision.Produit.Nom,
                        typeRisque: prevision.TypeRisquePredit.ToString(),
                        scoreRisque: prevision.ScoreRisque,
                        quantiteActuelle: prevision.Produit.Stock.QuantiteActuelle,
                        typeAction: action.TypeAction.ToString()
                    );
                    
                    if (!string.IsNullOrEmpty(explication))
                    {
                        action.TexteGenere = explication;
                    }
                }
            }

            _context.ActionsRecommandees.Add(action);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAction), new { id = action.Id }, action);
        }

        // PUT: api/actionrecommandee/5/valider
        // Seul ADMIN peut valider ou rejeter une action recommandée
        [HttpPut("{id}/valider")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ValiderAction(int id, [FromBody] int utilisateurId)
        {
            var action = await _context.ActionsRecommandees.FindAsync(id);
            if (action == null)
                return NotFound();

            action.UtilisateurId = utilisateurId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/actionrecommandee/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerAction(int id)
        {
            var action = await _context.ActionsRecommandees.FindAsync(id);
            if (action == null)
                return NotFound();

            _context.ActionsRecommandees.Remove(action);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}