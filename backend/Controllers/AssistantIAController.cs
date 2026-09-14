using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using WicStock_.Services;

namespace WicStock_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/assistant")]
    public class AssistantIAController : ControllerBase
    {
        private readonly HttpClient _httpIA;
        private readonly ILogger<AssistantIAController> _logger;

        public AssistantIAController(IHttpClientFactory httpClientFactory, ILogger<AssistantIAController> logger)
        {
            _httpIA = httpClientFactory.CreateClient("WicStockIA");
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] object request)
        {
            try
            {
                _logger.LogInformation("Transmission requête chat à l'Assistant IA FastAPI...");
                var response = await _httpIA.PostAsJsonAsync("chat", request);

                // Incrémenter la métrique custom Prometheus pour l'Observabilité Grafana
                WicStockMetrics.AiRequestsTotal.WithLabels("chat", ((int)response.StatusCode).ToString()).Inc();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                var content = await response.Content.ReadFromJsonAsync<object>();
                return Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la communication avec l'IA FastAPI.");
                WicStockMetrics.AiRequestsTotal.WithLabels("chat", "500").Inc();
                return StatusCode(500, new { message = "Le service IA n'est pas disponible actuellement." });
            }
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] object request)
        {
            try
            {
                _logger.LogInformation("Transmission requête ask à l'Assistant IA FastAPI...");
                var response = await _httpIA.PostAsJsonAsync("ask", request);

                // Incrémenter la métrique custom Prometheus pour l'Observabilité Grafana
                WicStockMetrics.AiRequestsTotal.WithLabels("ask", ((int)response.StatusCode).ToString()).Inc();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                var content = await response.Content.ReadFromJsonAsync<object>();
                return Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la communication avec l'IA FastAPI.");
                WicStockMetrics.AiRequestsTotal.WithLabels("ask", "500").Inc();
                return StatusCode(500, new { message = "Le service IA n'est pas disponible actuellement." });
            }
        }
    }
}
