using BambooCards.Application.Common;
using BambooCards.Application.InterfaceServiceClients;
using BambooCards.Application.Models.Response;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Runtime;

namespace BambooCards.Infrastructure.ServiceClients
{
    public class FrankfurterServiceClients : IFrankfuterServiceClients
    {
        private readonly HttpClient _httpClient;
        private readonly FrankfuterService _frankfuterService;
        public FrankfurterServiceClients(HttpClient httpClient, IOptions<FrankfuterService> frankfuterService)
        {
            _httpClient = httpClient;
            _frankfuterService = frankfuterService.Value;
        }
        public async Task<ExchangeRateResponse> GetLatestAsync(string? baseCurrency=null)
        {
            string url = string.Empty;
            if (baseCurrency== null)
            {
                url = _frankfuterService.LatestRates;
            }
            else
            {
                url = _frankfuterService.LatestRatesByBase + baseCurrency;
            }
            var data = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(url);

            if (data != null)
            {
               return data;
            }

            return new ExchangeRateResponse();

        }
        public async Task<string> GetCurrencyExchange(decimal amount,string fromCurrency, string toCurrency) 
        {
            string url = string.Format(_frankfuterService.CurrencyConversion, fromCurrency, toCurrency);

            var data = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(url);

            string convertedAmount = (amount * data.Rates[toCurrency]).ToString("F2");

            return $"Amount from  {fromCurrency} to Currency {toCurrency} : {convertedAmount}";
        }
        public async Task<GetHistoryBySymbolAndDateRangeResponse> GetHistoryBySymbolAndDateRange(string fromDate, string endDate, string symbol)
        {
            string url = string.Format(_frankfuterService.GetHistoryBySymbolAndDateRange, fromDate, endDate,symbol);

            var data = await _httpClient.GetFromJsonAsync<GetHistoryBySymbolAndDateRangeResponse>(url);
            if (data != null)
            {
                return data;
            }

            return new GetHistoryBySymbolAndDateRangeResponse();
        }
    }
}
