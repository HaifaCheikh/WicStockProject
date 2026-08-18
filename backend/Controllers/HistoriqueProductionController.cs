using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
    public class HistoriqueProductionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HistoriqueProductionController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/historiqueproduction
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HistoriqueProduction>>> GetHistoriqueProductions()
        {
            return await _context.HistoriqueProductions
                .Include(h => h.Produit)
                .OrderByDescending(h => h.DateProduction)
                .ToListAsync();
        }

        // GET: api/historiqueproduction/produit/3
        [HttpGet("produit/{produitId}")]
        public async Task<ActionResult<IEnumerable<HistoriqueProduction>>> GetParProduit(int produitId)
        {
            return await _context.HistoriqueProductions
                .Where(h => h.ProduitId == produitId)
                .OrderByDescending(h => h.DateProduction)
                .ToListAsync();
        }

        // POST: api/historiqueproduction
        [HttpPost]
        [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
        public async Task<ActionResult<HistoriqueProduction>> CreerProduction(HistoriqueProduction production)
        {
            _context.HistoriqueProductions.Add(production);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetParProduit), new { produitId = production.ProduitId }, production);
        }

        // DELETE: api/historiqueproduction/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerProduction(int id)
        {
            var production = await _context.HistoriqueProductions.FindAsync(id);
            if (production == null)
                return NotFound();

            _context.HistoriqueProductions.Remove(production);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}