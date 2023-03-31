using System;
using System.Text.Json.Serialization;

namespace Jyben.Racing.Dashboard.Shared.Models
{
    public class Circuit
    {
        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("zone")]
        public List<Zone> Zone { get; set; }
    }

    public class Coord
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public List<double> Y { get; set; }
    }

    public class CircuitsDto
    {
        [JsonPropertyName("circuits")]
        public List<Circuit> Circuits { get; set; }
    }

    public class Zone
    {
        [JsonPropertyName("secteur")]
        public int Secteur { get; set; }

        [JsonPropertyName("coord")]
        public Coord Coord { get; set; }

        [JsonPropertyName("sens")]
        public int Sens { get; set; }
    }
}

