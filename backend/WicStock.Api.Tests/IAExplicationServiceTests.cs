using System.Net;
using FluentAssertions;
using Moq;
using Moq.Protected;
using WicStock_.Services;
using Xunit;

namespace WicStock.Api.Tests
{
    public class IAExplicationServiceTests
    {
        [Fact]
        public async Task GenererExplication_WhenApiReturnsSuccess_ShouldReturnGeneratedText()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var jsonResponse = "{\"texte_genere\":\"Stock excessif détecté. Organiser une vente flash.\"}";
            
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8001/")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("WicStockIA")).Returns(httpClient);

            var service = new IAExplicationService(factoryMock.Object);

            // Act
            var result = await service.GenererExplication("T-Shirt Coton", "Surstock", 0.85f, 500, "PROMOTION");

            // Assert
            result.Should().Be("Stock excessif détecté. Organiser une vente flash.");
        }

        [Fact]
        public async Task GenererExplication_WhenApiReturnsError_ShouldReturnNull()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8001/")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("WicStockIA")).Returns(httpClient);

            var service = new IAExplicationService(factoryMock.Object);

            // Act
            var result = await service.GenererExplication("T-Shirt Coton", "Surstock", 0.85f, 500, "PROMOTION");

            // Assert
            result.Should().BeNull();
        }
    }
}
