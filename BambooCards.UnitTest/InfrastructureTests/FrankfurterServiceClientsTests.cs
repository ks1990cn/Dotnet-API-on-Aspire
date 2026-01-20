using BambooCards.Application.Common;
using BambooCards.Application.Models.Response;
using BambooCards.Infrastructure.ServiceClients;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BambooCards.UnitTest.InfrastructureTests
{
    public class FrankfurterServiceClientsTests
    {
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responder(request));
            }
        }

        private static FrankfurterServiceClients CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder, FrankfuterService config)
        {
            var handler = new MockHttpMessageHandler(responder);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.test/")
            };
            var options = Options.Create(config);
            return new FrankfurterServiceClients(httpClient, options);
        }

        [Fact]
        public async Task GetLatestAsync_NoBaseCurrency_ReturnsExchangeRateResponse()
        {
            var expected = new ExchangeRateResponse
            {
                BaseCurrency = "EUR",
                Date = "2025-01-01",
                Rates = new Dictionary<string, decimal> { { "USD", 1.1m } }
            };

            var client = CreateClient(req =>
            {
                var json = JsonSerializer.Serialize(expected);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            },
            new FrankfuterService
            {
                LatestRates = "latest"
            });

            var result = await client.GetLatestAsync();

            Assert.NotNull(result);
            Assert.Equal("EUR", result.BaseCurrency);
            Assert.True(result.Rates.ContainsKey("USD"));
            Assert.Equal(1.1m, result.Rates["USD"]);
        }

        [Fact]
        public async Task GetLatestAsync_WithBaseCurrency_UsesLatestRatesByBaseUrl()
        {
            var expected = new ExchangeRateResponse
            {
                BaseCurrency = "USD",
                Date = "2025-01-02",
                Rates = new Dictionary<string, decimal> { { "GBP", 0.75m } }
            };

            var config = new FrankfuterService
            {
                LatestRatesByBase = "latest?base="
            };

            var client = CreateClient(req =>
            {
                // verify that the request contains the base appended
                Assert.Contains("latest?base=USD", req.RequestUri.ToString(), StringComparison.OrdinalIgnoreCase);

                var json = JsonSerializer.Serialize(expected);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }, config);

            var result = await client.GetLatestAsync("USD");

            Assert.NotNull(result);
            Assert.Equal("USD", result.BaseCurrency);
            Assert.Equal(0.75m, result.Rates["GBP"]);
        }

        [Fact]
        public async Task GetCurrencyExchange_ReturnsFormattedConvertedAmount()
        {
            var ratesResponse = new ExchangeRateResponse
            {
                BaseCurrency = "EUR",
                Date = "2025-01-03",
                Rates = new Dictionary<string, decimal> { { "USD", 2.5m } }
            };

            var config = new FrankfuterService
            {
                CurrencyConversion = "convert?from={0}&to={1}"
            };

            var client = CreateClient(req =>
            {
                // ensure correct URL formation
                Assert.Contains("convert?from=EUR&to=USD", req.RequestUri.ToString(), StringComparison.OrdinalIgnoreCase);

                var json = JsonSerializer.Serialize(ratesResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }, config);

            var output = await client.GetCurrencyExchange(10m, "EUR", "USD");

            // 10 * 2.5 = 25.00
            Assert.Equal("Amount from  EUR to Currency USD : 25.00", output);
        }

        [Fact]
        public async Task GetHistoryBySymbolAndDateRange_ReturnsHistoryResponse()
        {
            var history = new GetHistoryBySymbolAndDateRangeResponse
            {
                Base = "EUR",
                StartDate = "2024-12-01",
                EndDate = "2024-12-31",
                Rates = new Dictionary<string, Dictionary<string, double>>
                {
                    { "2024-12-01", new Dictionary<string, double> { { "USD", 1.2 } } }
                }
            };

            var config = new FrankfuterService
            {
                GetHistoryBySymbolAndDateRange = "history?start={0}&end={1}&symbols={2}"
            };

            var client = CreateClient(req =>
            {
                // check url formatting
                Assert.Contains("history?start=2024-12-01&end=2024-12-31&symbols=USD", req.RequestUri.ToString(), StringComparison.OrdinalIgnoreCase);

                var json = JsonSerializer.Serialize(history);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }, config);

            var result = await client.GetHistoryBySymbolAndDateRange("2024-12-01", "2024-12-31", "USD");

            Assert.NotNull(result);
            Assert.Equal("EUR", result.Base);
            Assert.Equal("2024-12-01", result.StartDate);
            Assert.True(result.Rates.ContainsKey("2024-12-01"));
            Assert.Equal(1.2, result.Rates["2024-12-01"]["USD"]);
        }
    }
}
