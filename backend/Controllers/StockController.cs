using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "RESPONSABLE_STOCK_PRODUCTION,ADMIN")]
    public class StockController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/stock
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocks()
        {
            return await _context.Stocks
                .Include(s => s.Produit)
                .ToListAsync();
        }

        // GET: api/stock/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Stock>> GetStock(int id)
        {
            var stock = await _context.Stocks
                .Include(s => s.Produit)
                .Include(s => s.Mouvements)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock == null)
                return NotFound();

            return stock;
        }

        // GET: api/stock/sous-seuil
        [HttpGet("sous-seuil")]
        public async Task<ActionResult<IEnumerable<Stock>>> GetStocksSousSeuil()
        {
            var stocks = await _context.Stocks
                .Include(s => s.Produit)
                .Where(s => s.QuantiteActuelle < s.SeuilAlerte)
                .ToListAsync();

            return stocks;
        }

        // POST: api/stock
        [HttpPost]
        public async Task<ActionResult<Stock>> CreerStock(Stock stock)
        {
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStock), new { id = stock.Id }, stock);
        }

        // PUT: api/stock/5
        [HttpPut("{id}")]
        public async Task<IActionResult> ModifierStock(int id, Stock stock)
        {
            if (id != stock.Id)
                return BadRequest();

            stock.DateMiseAJour = DateTime.Now;
            _context.Entry(stock).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Stocks.Any(s => s.Id == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/stock/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> SupprimerStock(int id)
        {
            var stock = await _context.Stocks.FindAsync(id);
            if (stock == null)
                return NotFound();

            _context.Stocks.Remove(stock);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}