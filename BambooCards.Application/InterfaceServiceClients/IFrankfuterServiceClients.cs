using BambooCards.Application.Common;
using BambooCards.Application.Models.Response;

namespace BambooCards.Application.InterfaceServiceClients
{
    public interface IFrankfuterServiceClients
    {
        Task<string> GetCurrencyExchange(decimal amount, string fromCurrency, string toCurrency);
        Task<GetHistoryBySymbolAndDateRangeResponse> GetHistoryBySymbolAndDateRange(string fromDate, string endDate, string symbol);
        Task<ExchangeRateResponse> GetLatestAsync(string? baseCurrency = null);
    }
}
