using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DiagnosticController : ControllerBase
    {
        /// <summary>
        /// Retourne les informations du token JWT décodé
        /// </summary>
        [HttpGet("moi")]
        public IActionResult ObtenirMonToken()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                Name = User.Identity?.Name,
                Claims = claims,
                Roles = roles,
                HasAdminRole = User.IsInRole("ADMIN"),
                HasClientRole = User.IsInRole("CLIENT"),
                HasResponsableRole = User.IsInRole("RESPONSABLE_STOCK_PRODUCTION")
            });
        }
    }
}
