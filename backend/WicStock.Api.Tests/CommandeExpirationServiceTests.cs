using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;

using WicStock_.Hubs;
using WicStock_.Models;
using WicStock_.Services;
using Xunit;
using static WicStock_.Models.Enums;

namespace WicStock.Api.Tests
{
    public class CommandeExpirationServiceTests
    {
        private AppDbContext GetInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        private IServiceProvider CreateServiceProvider(string dbName)
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: dbName));

            var clientProxyMock = new Mock<IClientProxy>();
            var clientsMock = new Mock<IHubClients>();
            clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

            var hubContextMock = new Mock<IHubContext<NotificationHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

            services.AddScoped(_ => hubContextMock.Object);
            services.AddScoped<NotificationService>();

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task AnnulerCommandesExpireesAsync_ShouldCancelUnpaidOrdersOlderThan7DaysAndRestoreStock()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();

            var serviceProvider = CreateServiceProvider(dbName);

            using (var scope1 = serviceProvider.CreateScope())
            {
                var seedContext = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
                var produit = new Produit
                {
                    Id = 1,
                    Nom = "Jean Slim Indigo",
                    Reference = "JEAN-001",
                    PrixUnitaire = 89.90m
                };
                var stock = new Stock
                {
                    Id = 10,
                    ProduitId = 1,
                    QuantiteActuelle = 15,
                    SeuilAlerte = 5
                };
                seedContext.Produits.Add(produit);
                seedContext.Stocks.Add(stock);

                // 1. Order created 8 days ago, UNPAID -> SHOULD BE CANCELLED
                var commandeExpiree = new HistoriqueVente
                {
                    Id = 100,
                    ProduitId = 1,
                    QuantiteVendue = 5,
                    PrixUnitaire = 89.90m,
                    StatutCommande = "ACCEPTEE",
                    Statut = StatutCommandeDetaille.ACCEPTEE,
                    DateVente = DateTime.Now.AddDays(-8),
                    DatePaiement = null,
                    UtilisateurId = 1
                };

                // 2. Order created 2 days ago, UNPAID -> SHOULD NOT BE CANCELLED
                var commandeRecente = new HistoriqueVente
                {
                    Id = 101,
                    ProduitId = 1,
                    QuantiteVendue = 3,
                    PrixUnitaire = 89.90m,
                    StatutCommande = "ACCEPTEE",
                    Statut = StatutCommandeDetaille.ACCEPTEE,
                    DateVente = DateTime.Now.AddDays(-2),
                    DatePaiement = null,
                    UtilisateurId = 1
                };

                // 3. Order created 10 days ago, PAID -> SHOULD NOT BE CANCELLED
                var commandePayee = new HistoriqueVente
                {
                    Id = 102,
                    ProduitId = 1,
                    QuantiteVendue = 2,
                    PrixUnitaire = 89.90m,
                    StatutCommande = "ACCEPTEE",
                    Statut = StatutCommandeDetaille.PAYEE,
                    DateVente = DateTime.Now.AddDays(-10),
                    DatePaiement = DateTime.Now.AddDays(-9),
                    UtilisateurId = 1
                };

                seedContext.HistoriqueVentes.AddRange(commandeExpiree, commandeRecente, commandePayee);
                await seedContext.SaveChangesAsync();
            }

            var loggerMock = new Mock<ILogger<CommandeExpirationService>>();
            var expirationService = new CommandeExpirationService(serviceProvider, loggerMock.Object);

            // Act
            int count = await expirationService.AnnulerCommandesExpireesAsync(7);

            // Assert
            count.Should().Be(1);

            using (var scope2 = serviceProvider.CreateScope())
            {
                var verifyContext = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
                var updatedExpiree = await verifyContext.HistoriqueVentes.FindAsync(100);
                updatedExpiree.Should().NotBeNull();
                updatedExpiree!.StatutCommande.Should().Be("ANNULEE");
                updatedExpiree.Statut.Should().Be(StatutCommandeDetaille.ANNULEE);

                var updatedRecente = await verifyContext.HistoriqueVentes.FindAsync(101);
                updatedRecente!.StatutCommande.Should().Be("ACCEPTEE");

                var updatedPayee = await verifyContext.HistoriqueVentes.FindAsync(102);
                updatedPayee!.Statut.Should().Be(StatutCommandeDetaille.PAYEE);

                // Stock should be restored for commande 100 (+5) -> 15 + 5 = 20
                var updatedStock = await verifyContext.Stocks.FirstOrDefaultAsync(s => s.ProduitId == 1);
                updatedStock.Should().NotBeNull();
                updatedStock!.QuantiteActuelle.Should().Be(20);

                // MouvementStock ENTREE should be recorded
                var mouvement = await verifyContext.MouvementsStock.FirstOrDefaultAsync(m => m.StockId == 10);
                mouvement.Should().NotBeNull();
                mouvement!.Type.Should().Be(TypeMouvement.ENTREE);
                mouvement.Quantite.Should().Be(5);
                mouvement.Motif.Should().ContainEquivalentOf("restitution");
            }
        }
    }
}
