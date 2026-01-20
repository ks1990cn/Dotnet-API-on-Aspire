using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BambooCards.Application.Models.Response
{
    public class GetHistoryBySymbolAndDateRangeResponse
    {
        [JsonPropertyName("base")]
        public string Base { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; }

        /// <summary>
        /// Key: Date (e.g., "2023-12-29")
        /// Value: A dictionary where Key is Currency (e.g., "USD") and Value is the Rate.
        /// </summary>
        [JsonPropertyName("rates")]
        public Dictionary<string, Dictionary<string, double>> Rates { get; set; }
    }
}
