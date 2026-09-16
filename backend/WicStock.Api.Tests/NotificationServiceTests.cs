using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using WicStock_.Hubs;
using WicStock_.Models;
using WicStock_.Services;
using Xunit;
using static WicStock_.Models.Enums;

namespace WicStock.Api.Tests
{
    public class NotificationServiceTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task NotifierNouvelEvenementAsync_WithSpecificUser_ShouldSaveNotificationAndSendToUserGroup()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            
            var clientProxyMock = new Mock<IClientProxy>();
            var clientsMock = new Mock<IHubClients>();
            clientsMock.Setup(c => c.Group("user_42")).Returns(clientProxyMock.Object);

            var hubContextMock = new Mock<IHubContext<NotificationHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

            var service = new NotificationService(context, hubContextMock.Object);

            // Act
            var notif = await service.NotifierNouvelEvenementAsync(
                TypeNotification.ACTION_IA_A_VALIDER,
                "Alerte de surstock sur T-Shirt Denim",
                "/produits/1",
                utilisateurDestinataireId: 42
            );

            // Assert
            notif.Should().NotBeNull();
            notif.Id.Should().BeGreaterThan(0);
            notif.Message.Should().Be("Alerte de surstock sur T-Shirt Denim");

            var savedNotif = await context.Notifications.FirstOrDefaultAsync(n => n.Id == notif.Id);
            savedNotif.Should().NotBeNull();
            savedNotif!.UtilisateurDestinataireId.Should().Be(42);

            clientProxyMock.Verify(
                p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), default),
                Times.Once
            );
        }

        [Fact]
        public async Task NotifierNouvelEvenementAsync_WithStockRole_ShouldAlsoNotifyAdmin()
        {
            // Arrange
            using var context = GetInMemoryDbContext();

            var stockProxyMock = new Mock<IClientProxy>();
            var adminProxyMock = new Mock<IClientProxy>();
            var clientsMock = new Mock<IHubClients>();

            clientsMock.Setup(c => c.Group(RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION.ToString())).Returns(stockProxyMock.Object);
            clientsMock.Setup(c => c.Group(RoleUtilisateur.ADMIN.ToString())).Returns(adminProxyMock.Object);

            var hubContextMock = new Mock<IHubContext<NotificationHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

            var service = new NotificationService(context, hubContextMock.Object);

            // Act
            var notif = await service.NotifierNouvelEvenementAsync(
                TypeNotification.RUPTURE_STOCK,
                "Rupture critique imminente",
                "/stocks",
                roleDestinataire: RoleUtilisateur.RESPONSABLE_STOCK_PRODUCTION
            );

            // Assert
            notif.Should().NotBeNull();

            var allNotifications = await context.Notifications.ToListAsync();
            allNotifications.Should().HaveCount(2); // Should create notification for Stock Manager AND Admin
            allNotifications.Should().Contain(n => n.RoleDestinataire == RoleUtilisateur.ADMIN);

            adminProxyMock.Verify(
                p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), default),
                Times.Once
            );
        }
    }
}
