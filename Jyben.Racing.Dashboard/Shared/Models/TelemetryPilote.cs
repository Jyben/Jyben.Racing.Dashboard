using System;
using System.Text.Json.Serialization;

namespace Jyben.Racing.Dashboard.Shared.Models
{
    public class TelemetryPilote
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("trace")]
        public Trace Trace { get; set; }
    }
}

