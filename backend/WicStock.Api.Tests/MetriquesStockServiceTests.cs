using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using WicStock_.Services;
using Xunit;

namespace WicStock.Api.Tests
{
    public class MetriquesStockServiceTests
    {
        private AppDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task CalculerMetriquesProduitAsync_WhenProductNotFound_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateDbContext(Guid.NewGuid().ToString());
            var service = new MetriquesStockService(context);

            // Act & Assert
            Func<Task> act = async () => await service.CalculerMetriquesProduitAsync(999);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Produit non*");
        }

        [Fact]
        public async Task CalculerMetriquesProduitAsync_WhenStockExceedsThresholdAndInactiveMoreThan21Days_CalculatesOverstockPercentage()
        {
            // Arrange
            using var context = CreateDbContext(Guid.NewGuid().ToString());
            
            var produit = new Produit
            {
                Id = 1,
                Nom = "Tissu Lin Écologique",
                Reference = "LIN-001",
                Categorie = "Tissus",
                PrixUnitaire = 25m,
                DateCreation = DateTime.Now.AddDays(-60),
                Stock = new Stock
                {
                    QuantiteActuelle = 150, // Seuil par défaut = 100
                    SeuilAlerte = 10,
                    DateMiseAJour = DateTime.Now.AddDays(-30)
                },
                HistoriqueVentes = new List<HistoriqueVente>
                {
                    new HistoriqueVente
                    {
                        DateVente = DateTime.Now.AddDays(-30), // Inactif depuis 30 jours (> 21)
                        QuantiteVendue = 5,
                        PrixUnitaire = 25m,
                        MontantTotal = 125m
                    }
                }
            };

            context.Produits.Add(produit);
            await context.SaveChangesAsync();

            var service = new MetriquesStockService(context);

            // Act
            var result = await service.CalculerMetriquesProduitAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.ProduitId.Should().Be(1);
            result.StockActuel.Should().Be(150);
            result.SeuilSurstock.Should().Be(100);
            result.PourcentageAuDessusDuSeuil.Should().Be(50.0); // (150 - 100) / 100 * 100 = 50%
            result.JoursDepuisDerniereSortie.Should().BeGreaterThanOrEqualTo(29);
        }

        [Fact]
        public async Task CalculerMetriquesProduitAsync_WhenInactiveLessThan21Days_EnforcesAntiFalsePositiveRuleAndResetsOverstockPercentage()
        {
            // Arrange
            using var context = CreateDbContext(Guid.NewGuid().ToString());

            var produit = new Produit
            {
                Id = 2,
                Nom = "Fil de Coton Recyclé",
                Reference = "FIL-002",
                Categorie = "Fils",
                PrixUnitaire = 10m,
                DateCreation = DateTime.Now.AddDays(-40),
                Stock = new Stock
                {
                    QuantiteActuelle = 200,
                    SeuilAlerte = 10,
                    DateMiseAJour = DateTime.Now.AddDays(-5)
                },
                HistoriqueVentes = new List<HistoriqueVente>
                {
                    new HistoriqueVente
                    {
                        DateVente = DateTime.Now.AddDays(-5), // Inactif depuis seulement 5 jours (< 21 jours)
                        QuantiteVendue = 20,
                        PrixUnitaire = 10m,
                        MontantTotal = 200m
                    }
                }
            };

            context.Produits.Add(produit);
            await context.SaveChangesAsync();

            var service = new MetriquesStockService(context);

            // Act
            var result = await service.CalculerMetriquesProduitAsync(2);

            // Assert
            result.Should().NotBeNull();
            result.StockActuel.Should().Be(200);
            result.JoursDepuisDerniereSortie.Should().BeLessThan(21);
            result.PourcentageAuDessusDuSeuil.Should().Be(0); // Anti-false positive rule sets to 0
        }

        [Fact]
        public async Task CalculerMetriquesProduitAsync_WhenCustomThresholdSet_UsesCustomSeuilSurstock()
        {
            // Arrange
            using var context = CreateDbContext(Guid.NewGuid().ToString());

            var produit = new Produit
            {
                Id = 3,
                Nom = "Boutons Nacre Recyclée",
                Reference = "BTN-003",
                Categorie = "Accessoires",
                PrixUnitaire = 2m,
                DateCreation = DateTime.Now.AddDays(-100),
                Stock = new Stock
                {
                    QuantiteActuelle = 300,
                    SeuilAlerte = 200, // Custom threshold > 50 -> uses 200
                    DateMiseAJour = DateTime.Now.AddDays(-40)
                },
                HistoriqueVentes = new List<HistoriqueVente>
                {
                    new HistoriqueVente
                    {
                        DateVente = DateTime.Now.AddDays(-40),
                        QuantiteVendue = 10,
                        PrixUnitaire = 2m,
                        MontantTotal = 20m
                    }
                }
            };

            context.Produits.Add(produit);
            await context.SaveChangesAsync();

            var service = new MetriquesStockService(context);

            // Act
            var result = await service.CalculerMetriquesProduitAsync(3);

            // Assert
            result.Should().NotBeNull();
            result.SeuilSurstock.Should().Be(200);
            result.PourcentageAuDessusDuSeuil.Should().Be(50.0); // (300 - 200) / 200 * 100 = 50%
        }
    }
}
