using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("Jyben.Racing.Dashboard.Client")]
namespace Jyben.Racing.Dashboard.Shared.Models
{

    public class TelemetryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("tours")]
        public List<Tour> Tours { get; set; }

        [JsonPropertyName("traces")]
        public List<Trace> Traces { get; set; }
    }

    public class Secteur
    {
        [JsonPropertyName("numSecteur")]
        public int NumSecteur { get; set; }

        [JsonPropertyName("temps")]
        public string Temps { get; set; }

        /// <summary>
        /// Sert à calculer le temps du secteur
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime DatePassage { get; set; }
    }

    public class Tour
    {
        [JsonPropertyName("numTour")]
        public int NumTour { get; set; }

        [JsonPropertyName("temps")]
        public string Temps { get; set; }

        [JsonPropertyName("secteur")]
        public List<Secteur> Secteurs { get; set; }
    }

    public class Trace
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("vitesse")]
        public int Vitesse { get; set; }

        [JsonIgnore]
        internal DateTime Time { get; set; }
    }
}
    
