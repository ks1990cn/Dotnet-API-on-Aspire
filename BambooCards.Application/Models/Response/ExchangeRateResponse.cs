using System.Text.Json.Serialization;

namespace BambooCards.Application.Models.Response
{
    public class ExchangeRateResponse
    {
        [JsonPropertyName("base")]
        public string BaseCurrency { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
