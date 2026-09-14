using Prometheus;

namespace WicStock_.Services
{
    /// <summary>
    /// Métriques applicatives personnalisées exposées au format Prometheus pour WicStock.
    /// </summary>
    public static class WicStockMetrics
    {
        /// <summary>
        /// Compteur du nombre de requêtes envoyées vers le microservice IA FastAPI.
        /// </summary>
        public static readonly Counter AiRequestsTotal = Metrics
            .CreateCounter("wicstock_ai_requests_total", "Nombre total de requêtes envoyées au service IA FastAPI", new CounterConfiguration
            {
                LabelNames = new[] { "endpoint", "status" }
            });

        /// <summary>
        /// Jauge représentant le nombre total de produits enregistrés dans le catalogue.
        /// </summary>
        public static readonly Gauge ProductsTotal = Metrics
            .CreateGauge("wicstock_products_total", "Nombre total de produits en catalogue");
    }
}
