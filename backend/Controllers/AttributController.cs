using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WicStock_.Services;
using WicStock_.Models;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/attributs")]
    public class AttributController : ControllerBase
    {
        private readonly IAttributService _attributService;

        public AttributController(IAttributService attributService)
        {
            _attributService = attributService;
        }

        /// <summary>
        /// Obtient la liste des valeurs d'attributs pour un type donné (Genre, Taille, Couleur).
        /// Accessible publiquement pour alimenter la boutique et les filtres.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenirParType([FromQuery] string type, [FromQuery] bool tous = false)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return BadRequest(new { message = "Le paramètre 'type' est requis (Genre, Taille, Couleur)." });
            }

            var result = await _attributService.ObtenirParTypeAsync(type, uniquementActifs: !tous);
            return Ok(result);
        }

        /// <summary>
        /// Ajoute une nouvelle valeur d'attribut (Admin / Responsable).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ADMIN,RESPONSABLE_STOCK_PRODUCTION")]
        public async Task<IActionResult> Ajouter([FromBody] AttributValeur dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Type) || string.IsNullOrWhiteSpace(dto.Valeur))
            {
                return BadRequest(new { message = "Le type et la valeur sont requis." });
            }

            try
            {
                var attr = await _attributService.AjouterAsync(dto);
                return CreatedAtAction(nameof(ObtenirParType), new { type = attr.Type }, attr);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// Modifie une valeur d'attribut (Admin / Responsable).
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN,RESPONSABLE_STOCK_PRODUCTION")]
        public async Task<IActionResult> Modifier(int id, [FromBody] AttributValeur dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Valeur))
            {
                return BadRequest(new { message = "La valeur est requise." });
            }

            var succes = await _attributService.ModifierAsync(id, dto.Valeur, dto.CodeHex, dto.Ordre, dto.Actif);
            if (!succes) return NotFound(new { message = "Attribut introuvable." });

            return Ok(new { message = "Attribut mis à jour." });
        }

        /// <summary>
        /// Supprime ou désactive (soft-delete) un attribut (Admin / Responsable).
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN,RESPONSABLE_STOCK_PRODUCTION")]
        public async Task<IActionResult> Supprimer(int id)
        {
            var succes = await _attributService.SupprimerOuDesactiverAsync(id);
            if (!succes) return NotFound(new { message = "Attribut introuvable." });

            return Ok(new { message = "Attribut supprimé ou désactivé." });
        }
    }
}
