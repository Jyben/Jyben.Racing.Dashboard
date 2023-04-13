using System;
using System.Text.Json.Serialization;

namespace Jyben.Racing.Dashboard.Shared.Models
{
    public class Circuit
    {
        [JsonPropertyName("nom")]
        public string Nom { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("canevas")]
        public Canevas Canevas { get; set; }

        [JsonPropertyName("zones")]
        public List<Zone> Zone { get; set; }
    }

    public class Canevas
    {
        [JsonPropertyName("width")]
        public long Width { get; set; }

        [JsonPropertyName("height")]
        public long Height { get; set; }

        [JsonPropertyName("top-left")]
        public CoordGPS TopLeft { get; set; }

        [JsonPropertyName("bottom-right")]
        public CoordGPS BottomRight { get; set; }
    }

    public class CoordGPS
    {
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
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

