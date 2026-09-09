using Microsoft.EntityFrameworkCore;
using WicStock_.Models;

namespace WicStock_.Services
{
    public interface IAttributService
    {
        Task<List<AttributValeur>> ObtenirParTypeAsync(string type, bool uniquementActifs = true);
        Task<bool> EstValideAsync(string type, string? valeur);
        Task<AttributValeur> AjouterAsync(AttributValeur attr);
        Task<bool> ModifierAsync(int id, string valeur, string? codeHex, int ordre, bool actif);
        Task<bool> SupprimerOuDesactiverAsync(int id);
    }

    public class AttributService : IAttributService
    {
        private readonly AppDbContext _context;

        public AttributService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AttributValeur>> ObtenirParTypeAsync(string type, bool uniquementActifs = true)
        {
            var query = _context.AttributsValeurs.AsNoTracking()
                .Where(a => a.Type.ToLower() == type.ToLower());

            if (uniquementActifs)
            {
                query = query.Where(a => a.Actif);
            }

            return await query.OrderBy(a => a.Ordre).ThenBy(a => a.Valeur).ToListAsync();
        }

        public async Task<bool> EstValideAsync(string type, string? valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return true; // Valeur optionnelle autorisée

            return await _context.AttributsValeurs.AsNoTracking()
                .AnyAsync(a => a.Type.ToLower() == type.ToLower() 
                            && a.Valeur.ToLower() == valeur.Trim().ToLower() 
                            && a.Actif);
        }

        public async Task<AttributValeur> AjouterAsync(AttributValeur attr)
        {
            attr.Type = attr.Type.Trim();
            attr.Valeur = attr.Valeur.Trim();
            attr.Actif = true;

            _context.AttributsValeurs.Add(attr);
            await _context.SaveChangesAsync();
            return attr;
        }

        public async Task<bool> ModifierAsync(int id, string valeur, string? codeHex, int ordre, bool actif)
        {
            var attr = await _context.AttributsValeurs.FindAsync(id);
            if (attr == null) return false;

            attr.Valeur = valeur.Trim();
            attr.CodeHex = codeHex?.Trim();
            attr.Ordre = ordre;
            attr.Actif = actif;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SupprimerOuDesactiverAsync(int id)
        {
            var attr = await _context.AttributsValeurs.FindAsync(id);
            if (attr == null) return false;

            // Vérifier s'il est référencé dans au moins une VarianteProduit
            bool estUtilise = false;
            if (attr.Type.Equals("Genre", StringComparison.OrdinalIgnoreCase))
                estUtilise = await _context.VariantesProduit.AnyAsync(v => v.Genre == attr.Valeur);
            else if (attr.Type.Equals("Taille", StringComparison.OrdinalIgnoreCase))
                estUtilise = await _context.VariantesProduit.AnyAsync(v => v.Taille == attr.Valeur);
            else if (attr.Type.Equals("Couleur", StringComparison.OrdinalIgnoreCase))
                estUtilise = await _context.VariantesProduit.AnyAsync(v => v.Couleur == attr.Valeur);

            if (estUtilise)
            {
                // Soft-delete pour préserver l'intégrité
                attr.Actif = false;
            }
            else
            {
                _context.AttributsValeurs.Remove(attr);
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
