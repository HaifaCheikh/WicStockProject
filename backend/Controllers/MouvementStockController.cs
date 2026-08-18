using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using static WicStock_.Models.Enums;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
    public class MouvementStockController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MouvementStockController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/mouvementstock
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MouvementStock>>> GetMouvements()
        {
            return await _context.MouvementsStock
                .Include(m => m.Stock)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }

        // GET: api/mouvementstock/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MouvementStock>> GetMouvement(int id)
        {
            var mouvement = await _context.MouvementsStock
                .Include(m => m.Stock)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mouvement == null)
                return NotFound();

            return mouvement;
        }

        // GET: api/mouvementstock/stock/3
        [HttpGet("stock/{stockId}")]
        public async Task<ActionResult<IEnumerable<MouvementStock>>> GetMouvementsParStock(int stockId)
        {
            return await _context.MouvementsStock
                .Where(m => m.StockId == stockId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }

        // POST: api/mouvementstock
        // Crée le mouvement ET met à jour la quantité du stock correspondant
        [HttpPost]
        public async Task<ActionResult<MouvementStock>> CreerMouvement(MouvementStock mouvement)
        {
            var stock = await _context.Stocks.FindAsync(mouvement.StockId);
            if (stock == null)
                return NotFound("Stock introuvable.");

            switch (mouvement.Type)
            {
                case TypeMouvement.ENTREE:
                case TypeMouvement.RETOUR:
                    stock.QuantiteActuelle += mouvement.Quantite;
                    break;
                case TypeMouvement.SORTIE:
                    stock.QuantiteActuelle -= mouvement.Quantite;
                    break;
                case TypeMouvement.AJUSTEMENT:
                    stock.QuantiteActuelle = mouvement.Quantite;
                    break;
            }

            stock.DateMiseAJour = DateTime.Now;

            _context.MouvementsStock.Add(mouvement);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMouvement), new { id = mouvement.Id }, mouvement);
        }

        // DELETE: api/mouvementstock/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SupprimerMouvement(int id)
        {
            var mouvement = await _context.MouvementsStock.FindAsync(id);
            if (mouvement == null)
                return NotFound();

            _context.MouvementsStock.Remove(mouvement);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}