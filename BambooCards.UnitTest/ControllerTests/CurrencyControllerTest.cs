using BambooCards.Application.InterfaceServiceClients;
using BambooCards.Application.Models.Response;
using BambooCards.Assessment.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;

namespace BambooCards.UnitTest.ControllerTests
{

        public class CurrencyConverterControllerTests
        {
            private readonly Mock<IFrankfuterServiceClients> _serviceMock;
            private readonly Mock<IDistributedCache> _cacheMock;
            private readonly CurrencyConverterController _controller;

            public CurrencyConverterControllerTests()
            {
                _serviceMock = new Mock<IFrankfuterServiceClients>(MockBehavior.Strict);
                _cacheMock = new Mock<IDistributedCache>(MockBehavior.Strict);

                _controller = new CurrencyConverterController(_serviceMock.Object, _cacheMock.Object);
            }

            [Fact]
            public async Task GetLatestExchangeRates_ReturnsOk_WithServiceResult()
            {
                // Arrange
                var expected = new ExchangeRateResponse
                {
                    BaseCurrency = "EUR",
                    Date = "2025-01-01",
                    Rates = new Dictionary<string, decimal> { { "USD", 1.23m }, { "GBP", 0.88m } }
                };

                _serviceMock.Setup(s => s.GetLatestAsync(It.IsAny<string?>()))
                            .ReturnsAsync(expected);

                // Act
                var result = await _controller.GetLatestExchangeRates(null);

                // Assert
                var ok = Assert.IsType<OkObjectResult>(result.Result);
                Assert.Same(expected, ok.Value);
                _serviceMock.Verify(s => s.GetLatestAsync(It.IsAny<string?>()), Times.Once);
            }

            [Fact]
            public async Task GetCurrencyExchange_RestrictedCurrency_ReturnsBadRequest()
            {
                // Arrange
                const string from = "TRY"; // restricted
                const string to = "USD";
                const decimal amount = 100;

                // Act
                var result = await _controller.GetCurrencyExchange(from, to, amount);

                // Assert
                var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
                var message = Assert.IsType<string>(bad.Value);
                Assert.Contains("TRY", message, StringComparison.OrdinalIgnoreCase);
                // service and cache should not be called
                _serviceMock.VerifyNoOtherCalls();
                _cacheMock.VerifyNoOtherCalls();
            }

            [Fact]
            public async Task GetCurrencyExchange_CacheHit_ReturnsCachedValue_And_DoesNotCallService()
            {
                // Arrange
                const string from = "EUR";
                const string to = "USD";
                const decimal amount = 10;
                string cacheKey = $"currency_{from}_{to}_{amount}";
                string cachedValue = "cached-result";

                // Setup: GetAsync should return encoded cachedValue
                _cacheMock.Setup(c => c.GetAsync(
                                    It.Is<string>(k => k == cacheKey),
                                    It.IsAny<CancellationToken>()))
                          .ReturnsAsync(Encoding.UTF8.GetBytes(cachedValue));

                // Act
                var result = await _controller.GetCurrencyExchange(from, to, amount);

                // Assert
                var ok = Assert.IsType<OkObjectResult>(result.Result);
                Assert.Equal(cachedValue, ok.Value);
                // Verify service not called
                _serviceMock.Verify(s => s.GetCurrencyExchange(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                _cacheMock.Verify(c => c.GetAsync(It.Is<string>(k => k == cacheKey), It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task GetCurrencyExchange_CacheMiss_CallsService_SetsCache_AndReturnsResult()
            {
                // Arrange
                const string from = "EUR";
                const string to = "USD";
                const decimal amount = 25;
                string cacheKey = $"currency_{from}_{to}_{amount}";
                string serviceResult = "Amount from  EUR to Currency USD : 30.00";

                // Cache miss -> GetAsync returns null
                _cacheMock.Setup(c => c.GetAsync(
                                    It.Is<string>(k => k == cacheKey),
                                    It.IsAny<CancellationToken>()))
                          .ReturnsAsync((byte[]?)null);

                // Service returns result
                _serviceMock.Setup(s => s.GetCurrencyExchange(amount, from, to))
                            .ReturnsAsync(serviceResult);

                // Expect SetAsync to be called with the encoded value
                _cacheMock.Setup(c => c.SetAsync(
                                    It.Is<string>(k => k == cacheKey),
                                    It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == serviceResult),
                                    It.IsAny<DistributedCacheEntryOptions>(),
                                    It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

                // Act
                var result = await _controller.GetCurrencyExchange(from, to, amount);

                // Assert
                var ok = Assert.IsType<OkObjectResult>(result.Result);
                Assert.Equal(serviceResult, ok.Value);

                _cacheMock.Verify(c => c.GetAsync(It.Is<string>(k => k == cacheKey), It.IsAny<CancellationToken>()), Times.Once);
                _serviceMock.Verify(s => s.GetCurrencyExchange(amount, from, to), Times.Once);
                _cacheMock.Verify(c => c.SetAsync(
                                        It.Is<string>(k => k == cacheKey),
                                        It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == serviceResult),
                                        It.IsAny<DistributedCacheEntryOptions>(),
                                        It.IsAny<CancellationToken>()),
                                  Times.Once);
            }

            [Fact]
            public async Task GetHistory_FormatsDatesAndReturnsOk()
            {
                // Arrange
                var fromDate = new DateTime(2025, 01, 01);
                DateTime? toDate = new DateTime(2025, 01, 05);
                const string symbol = "USD";

                var expectedResponse = new GetHistoryBySymbolAndDateRangeResponse
                {
                    Base = "EUR",
                    StartDate = fromDate.ToString("yyyy-MM-dd"),
                    EndDate = toDate.Value.ToString("yyyy-MM-dd"),
                    Rates = new Dictionary<string, Dictionary<string, double>>()
                };

                // Setup service to verify formatted strings are passed (we'll accept any here)
                _serviceMock.Setup(s => s.GetHistoryBySymbolAndDateRange(
                                        It.Is<string>(s => s == fromDate.ToString("yyyy-MM-dd")),
                                        It.Is<string>(e => e == toDate.Value.ToString("yyyy-MM-dd")),
                                        It.Is<string>(sym => sym == symbol)))
                            .ReturnsAsync(expectedResponse);

                // Act
                var actionResult = await _controller.GetHistory(fromDate, toDate, symbol);

                // Assert
                var ok = Assert.IsType<OkObjectResult>(actionResult);
                Assert.Same(expectedResponse, ok.Value);

                _serviceMock.Verify(s => s.GetHistoryBySymbolAndDateRange(
                                        fromDate.ToString("yyyy-MM-dd"),
                                        toDate.Value.ToString("yyyy-MM-dd"),
                                        symbol), Times.Once);
            }
        }
   
}
