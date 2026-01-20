using BambooCards.Application.InterfaceServiceClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BambooCards.Assessment.Controllers
{
    [ApiController]
    [Authorize]
    public class CurrencyConverterController : ControllerBase
    {

        private readonly IFrankfuterServiceClients _frankfuterServiceClients;
        private readonly IDistributedCache _cache;
        private static readonly HashSet<string> RestrictedCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "TRY", "PLN", "THB", "MXN"
        };
        public CurrencyConverterController(IFrankfuterServiceClients frankfuterServiceClients,
          IDistributedCache cache)
        {
            _frankfuterServiceClients = frankfuterServiceClients;
            _cache = cache;
        }

        [HttpGet("/GetLatestExchangeRates")]
        [EnableRateLimiting("fixed")]
        [Authorize(Policy = "BambooUserPolicy")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetLatestExchangeRates([FromQuery] string? baseCurrency)
        {
            try
            {
                var result = await _frankfuterServiceClients.GetLatestAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
            
        }
        [HttpGet("/GetCurrencyExchange")]
        [Authorize(Policy = "BambooUserPolicy")]
        public async Task<ActionResult<string>> GetCurrencyExchange([FromQuery] string fromCurrency,[FromQuery]string toCurrency, [FromQuery] decimal amount)
        {
            try
            {
                if (RestrictedCurrencies.Contains(fromCurrency) || RestrictedCurrencies.Contains(toCurrency))
                {
                    return BadRequest($"Currencies {string.Join(", ", RestrictedCurrencies)} are not supported.");
                }

                string cacheKey = $"currency_{fromCurrency}_{toCurrency}_{amount}";

                var cachedResult = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedResult))
                {
                    return Ok(cachedResult);
                }

                var result = await _frankfuterServiceClients.GetCurrencyExchange(amount, fromCurrency, toCurrency);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3)
                };

                await _cache.SetStringAsync(cacheKey, result, cacheOptions);

                return Ok(result);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
          
        }

        [HttpGet("/GetHistoryBySymbolAndDateRange")]
        [Authorize(Policy = "BambooUserPolicy")]
        public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string symbol)
        {
            try
            { // 1. Format dates to yyyy-MM-dd as required by Frankfurter
                string start = fromDate.ToString("yyyy-MM-dd");
                string end = toDate == null ? string.Empty : toDate.Value.ToString("yyyy-MM-dd");

                var result = await _frankfuterServiceClients.GetHistoryBySymbolAndDateRange(start, end, symbol);

                return Ok(result);

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
           
        }

    }
}
