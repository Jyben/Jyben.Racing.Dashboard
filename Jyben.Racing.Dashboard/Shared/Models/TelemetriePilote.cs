using System.Text.Json.Serialization;

namespace Jyben.Racing.Dashboard.Shared.Models
{
    public class TelemetriePilote
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("trace")]
        public Trace Trace { get; set; }
    }
}

