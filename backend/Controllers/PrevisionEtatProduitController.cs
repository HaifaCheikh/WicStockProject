using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
    public class PrevisionEtatProduitController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PrevisionEtatProduitController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/previsionetatproduit
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PrevisionEtatProduit>>> GetPrevisions()
        {
            return await _context.PrevisionsEtatProduit
                .Include(p => p.Produit)
                .Include(p => p.ActionRecommandee)
                .OrderByDescending(p => p.DateCalcul)
                .ToListAsync();
        }

        // GET: api/previsionetatproduit/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PrevisionEtatProduit>> GetPrevision(int id)
        {
            var prevision = await _context.PrevisionsEtatProduit
                .Include(p => p.Produit)
                .Include(p => p.ActionRecommandee)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prevision == null)
                return NotFound();

            return prevision;
        }

        // GET: api/previsionetatproduit/a-risque
        // Utile pour le tableau de bord Streamlit : les produits avec un score de risque élevé
        [HttpGet("a-risque")]
        public async Task<ActionResult<IEnumerable<PrevisionEtatProduit>>> GetProduitsARisque([FromQuery] float seuil = 0.7f)
        {
            return await _context.PrevisionsEtatProduit
                .Include(p => p.Produit)
                .Where(p => p.ScoreRisque >= seuil)
                .OrderByDescending(p => p.ScoreRisque)
                .ToListAsync();
        }

        // POST: api/previsionetatproduit
        // Endpoint appelé par le microservice FastAPI après calcul de la prévision
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<ActionResult<PrevisionEtatProduit>> CreerPrevision(PrevisionEtatProduit prevision)
        {
            _context.PrevisionsEtatProduit.Add(prevision);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPrevision), new { id = prevision.Id }, prevision);
        }

        // DELETE: api/previsionetatproduit/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerPrevision(int id)
        {
            var prevision = await _context.PrevisionsEtatProduit.FindAsync(id);
            if (prevision == null)
                return NotFound();

            _context.PrevisionsEtatProduit.Remove(prevision);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}