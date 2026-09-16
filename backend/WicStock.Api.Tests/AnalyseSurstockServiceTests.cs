using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WicStock_.Models.Dtos;
using WicStock_.Services;
using Xunit;

namespace WicStock.Api.Tests
{
    public class AnalyseSurstockServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

        public AnalyseSurstockServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            
            // Setup a mock HttpClient that throws/fails to trigger degraded mode
            var client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:8001/")
            };
            _httpClientFactoryMock.Setup(f => f.CreateClient("WicStockAI")).Returns(client);
        }

        [Fact]
        public async Task AnalyserSurstockAsync_WhenAiServiceFails_ActivatesDegradedModeAndGeneratesFallbackActions()
        {
            // Arrange
            var service = new AnalyseSurstockService(_httpClientFactoryMock.Object);
            var metriques = new MetriquesStockDto
            {
                ProduitId = 42,
                NomProduit = "Veste Denim Recyclé",
                StockActuel = 150,
                SeuilSurstock = 100,
                PourcentageAuDessusDuSeuil = 50.0,
                JoursDepuisDerniereSortie = 35,
                Categorie = "Vestes",
                EstTendanceCategorie = true,
                NbReferencesSimilairesEnSurstock = 3,
                ValeurStockImmobilisee = 4500m,
                CoutPossessionEstimeMensuel = 225m
            };

            // Act
            var result = await service.AnalyserSurstockAsync(metriques);

            // Assert
            result.Should().NotBeNull();
            result.ProduitId.Should().Be(42);
            result.EstModeDegrade.Should().BeTrue();
            result.Diagnostic.Should().Contain("Veste Denim Recyclé");
            result.Diagnostic.Should().Contain("150");
            result.Actions.Should().NotBeEmpty();
            result.Actions.Should().HaveCount(3);
        }

        [Fact]
        public async Task GenererActionsFallback_WhenSurplusExists_IncludesPromotionAndRecyclingActions()
        {
            // Arrange
            var service = new AnalyseSurstockService(_httpClientFactoryMock.Object);
            var metriques = new MetriquesStockDto
            {
                ProduitId = 10,
                NomProduit = "Pantalon Coton Bio",
                StockActuel = 120,
                SeuilSurstock = 100,
                PourcentageAuDessusDuSeuil = 20.0,
                JoursDepuisDerniereSortie = 40,
                Categorie = "Pantalons",
                EstTendanceCategorie = false,
                NbReferencesSimilairesEnSurstock = 0,
                ValeurStockImmobilisee = 2400m,
                CoutPossessionEstimeMensuel = 120m
            };

            // Act
            var result = await service.AnalyserSurstockAsync(metriques);

            // Assert
            result.Actions.Should().Contain(a => a.TypeAction == "PROMOTION_CIBLEE");
            result.Actions.Should().Contain(a => a.TypeAction == "RECYCLAGE_ANTICIPE");
            result.Actions.Should().Contain(a => a.TypeAction == "NOTIFICATION_PRODUCTION");

            var promoAction = result.Actions.Find(a => a.TypeAction == "PROMOTION_CIBLEE");
            promoAction.Should().NotBeNull();
            promoAction!.Params.Should().ContainKey("remisePourcentage").WhoseValue.Should().Be(20);
            promoAction.Params.Should().ContainKey("quantiteCible").WhoseValue.Should().Be(20); // surplus = 120 - 100 = 20
        }

        [Fact]
        public async Task GenererActionsFallback_WhenCategoryIsTrending_SetsHighPriorityNotification()
        {
            // Arrange
            var service = new AnalyseSurstockService(_httpClientFactoryMock.Object);
            var metriques = new MetriquesStockDto
            {
                ProduitId = 15,
                NomProduit = "Chemise Lin Premium",
                StockActuel = 180,
                SeuilSurstock = 100,
                PourcentageAuDessusDuSeuil = 80.0,
                JoursDepuisDerniereSortie = 30,
                Categorie = "Chemises",
                EstTendanceCategorie = true,
                NbReferencesSimilairesEnSurstock = 5,
                ValeurStockImmobilisee = 5400m,
                CoutPossessionEstimeMensuel = 270m
            };

            // Act
            var result = await service.AnalyserSurstockAsync(metriques);

            // Assert
            var notifAction = result.Actions.Find(a => a.TypeAction == "NOTIFICATION_PRODUCTION");
            notifAction.Should().NotBeNull();
            notifAction!.Params.Should().ContainKey("priorite").WhoseValue.Should().Be("HAUTE");
        }

        [Fact]
        public async Task GenererActionsFallback_WhenCategoryIsNotTrending_SetsMediumPriorityNotification()
        {
            // Arrange
            var service = new AnalyseSurstockService(_httpClientFactoryMock.Object);
            var metriques = new MetriquesStockDto
            {
                ProduitId = 20,
                NomProduit = "T-Shirt Motif Eco",
                StockActuel = 110,
                SeuilSurstock = 100,
                PourcentageAuDessusDuSeuil = 10.0,
                JoursDepuisDerniereSortie = 25,
                Categorie = "T-Shirts",
                EstTendanceCategorie = false,
                NbReferencesSimilairesEnSurstock = 0,
                ValeurStockImmobilisee = 1100m,
                CoutPossessionEstimeMensuel = 55m
            };

            // Act
            var result = await service.AnalyserSurstockAsync(metriques);

            // Assert
            var notifAction = result.Actions.Find(a => a.TypeAction == "NOTIFICATION_PRODUCTION");
            notifAction.Should().NotBeNull();
            notifAction!.Params.Should().ContainKey("priorite").WhoseValue.Should().Be("MOYENNE");
        }
    }
}
