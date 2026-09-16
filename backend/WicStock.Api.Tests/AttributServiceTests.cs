using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WicStock_.Models;
using WicStock_.Services;
using Xunit;

namespace WicStock.Api.Tests
{
    public class AttributServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task ObtenirParTypeAsync_ShouldReturnActiveAttributes_OrderedByOrdreAndValeur()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            context.AttributsValeurs.AddRange(
                new AttributValeur { Id = 1, Type = "Taille", Valeur = "XL", Actif = true, Ordre = 2 },
                new AttributValeur { Id = 2, Type = "Taille", Valeur = "M", Actif = true, Ordre = 1 },
                new AttributValeur { Id = 3, Type = "Taille", Valeur = "S", Actif = false, Ordre = 0 }
            );
            await context.SaveChangesAsync();

            var service = new AttributService(context);

            // Act
            var activeTailles = await service.ObtenirParTypeAsync("Taille", uniquementActifs: true);

            // Assert
            activeTailles.Should().HaveCount(2);
            activeTailles[0].Valeur.Should().Be("M");
            activeTailles[1].Valeur.Should().Be("XL");
        }

        [Fact]
        public async Task SupprimerOuDesactiverAsync_WhenAttributeIsReferencedInVariante_ShouldSoftDelete()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var attr = new AttributValeur { Id = 10, Type = "Couleur", Valeur = "Rouge", Actif = true, Ordre = 1 };
            context.AttributsValeurs.Add(attr);
            context.VariantesProduit.Add(new VarianteProduit { Id = 100, Reference = "VAR-RED-1", Couleur = "Rouge" });
            await context.SaveChangesAsync();

            var service = new AttributService(context);

            // Act
            var result = await service.SupprimerOuDesactiverAsync(10);

            // Assert
            result.Should().BeTrue();
            var updatedAttr = await context.AttributsValeurs.FindAsync(10);
            updatedAttr.Should().NotBeNull();
            updatedAttr!.Actif.Should().BeFalse(); // Soft deleted
        }

        [Fact]
        public async Task SupprimerOuDesactiverAsync_WhenAttributeIsUnused_ShouldHardDelete()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var attr = new AttributValeur { Id = 20, Type = "Taille", Valeur = "XXL", Actif = true, Ordre = 1 };
            context.AttributsValeurs.Add(attr);
            await context.SaveChangesAsync();

            var service = new AttributService(context);

            // Act
            var result = await service.SupprimerOuDesactiverAsync(20);

            // Assert
            result.Should().BeTrue();
            var updatedAttr = await context.AttributsValeurs.FindAsync(20);
            updatedAttr.Should().BeNull(); // Hard deleted
        }
    }
}
