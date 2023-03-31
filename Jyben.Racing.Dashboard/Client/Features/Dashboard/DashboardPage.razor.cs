using System;
using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;
using Jyben.Racing.Dashboard.Client.Helpers;
using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jyben.Racing.Dashboard.Client.Features.Dashboard
{
	public partial class DashboardPage : IAsyncDisposable
	{
		[Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public HttpClient HttpClient { get; set; }

        private HubConnection? hubConnection;
		private Dictionary<int, TelemetryDto> _telemetryKvp = new();
		private List<Zone>? _zones;
        private static string _filePathCircuits = "/circuits.json";
        private Canvas2DContext _context;
        protected BECanvasComponent _canvasReference;
        private static double _minDistance = 10;
        private GpsToPixelConverter _gpsToPixelConverter;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            _context = await _canvasReference.CreateCanvas2DAsync();
        }

        protected override async Task OnInitializedAsync()
		{
            try
            {
                var result = await HttpClient.GetFromJsonAsync<CircuitsDto>("circuits.json");
                _zones = result?.Circuits.First(x => x.Nom == "cik").Zone;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Une erreur est survenue lors de la désérialisation du fichier JSON : {0}", ex.Message);
            }

            hubConnection = new HubConnectionBuilder()
				.WithUrl(NavigationManager.ToAbsoluteUri("/signalr/telemetry"))
				.Build();

			hubConnection.On<int, string>("RecevoirTelemetry", async (userId, json) =>
			{
                Console.WriteLine(json);

                try
                {
				    var telemetryPilote = JsonSerializer.Deserialize<TelemetryPilote>(json);
                    ArgumentNullException.ThrowIfNull(telemetryPilote);

				    MettreAJourTelemetry(telemetryPilote);

                    var telemetry = _telemetryKvp.FirstOrDefault(x => x.Key == telemetryPilote.Id);
                    if (telemetry.Value != null && telemetry.Value.Traces != null)
                    {
                        await DrawCanevas(telemetry.Value.Traces);
                        StateHasChanged();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
			});

			await hubConnection.StartAsync();

            _gpsToPixelConverter = new(400, 600, 0.210903, 47.943942, 0.213743, 47.941083);
        }

        private async Task DrawCanevas(List<Trace> traces)
        {
            var trace = traces.OrderByDescending(x => x.Time).First();
            var tracePrecedente = traces.OrderByDescending(x => x.Time).Skip(1).Take(1).First();

            // calcul la distance depuis la dernière coordonnée GPS
            var distance = CalculerDistance(tracePrecedente.Latitude, tracePrecedente.Longitude, trace.Latitude, trace.Longitude);

            // vérifie si la distance est supérieure au seuil minimal
            if (distance < _minDistance)
            {
                return;
            }

            if (_context != null && traces.Count >= 2)
            {
                // dessine la ligne en reliant chaque paire de coordonnées
                await _context.BeginPathAsync();

                // converti en les coordonnées GPS en pixels
                var point = _gpsToPixelConverter.Convert(traces[traces.Count - 2].Longitude, traces[traces.Count - 2].Latitude);
                await _context.MoveToAsync(point.X, point.Y);
                point = _gpsToPixelConverter.Convert(traces[traces.Count - 1].Longitude, traces[traces.Count - 1].Latitude);
                await _context.LineToAsync(point.X, point.Y);
                var couleur = "";
                // personnalise l'apparence de la ligne
                if (trace.Type == "freinage")
                {
                    couleur = "#FF0000";
                }
                else if (trace.Type == "acceleration")
                {
                    couleur = "#00FF00";
                }
                else
                {
                    couleur = "#FFFFFF";
                }
                await _context.SetStrokeStyleAsync(couleur);
                await _context.SetLineWidthAsync(1);
                await _context.StrokeAsync();
            }
        }

        public static double CalculerDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6378137.0; // Rayon de la Terre en mètres

            // Conversion des coordonnées en radians
            double radLat1 = lat1 * Math.PI / 180.0;
            double radLon1 = lon1 * Math.PI / 180.0;
            double radLat2 = lat2 * Math.PI / 180.0;
            double radLon2 = lon2 * Math.PI / 180.0;

            // Calcul de la distance entre les deux points en utilisant la formule de Haversine
            double dLat = radLat2 - radLat1;
            double dLon = radLon2 - radLon1;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(radLat1) * Math.Cos(radLat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;

            return distance;
        }

        private void MettreAJourTelemetry(TelemetryPilote? telemetry)
		{
			if (telemetry == null) return;

			if (!_telemetryKvp.Any(x => x.Key == telemetry.Id))
			{
                // initialiser la telemetry du pilote
                _telemetryKvp.Add(telemetry.Id, new TelemetryDto()
                {
                    Id = telemetry.Id,
                    Nom = telemetry.Nom,
                    Traces = new() { CreerTrace(telemetry.Trace) },
                    Tours = new() { CreerNouveauTour() }
                });
            }
			else
			{
                var telemetryKvp = _telemetryKvp.First(x => x.Key == telemetry.Id);
                var telemetryAModifier = telemetryKvp.Value;

                telemetryAModifier.Traces.Add(CreerTrace(telemetry.Trace));

                VerifierNouveauSecteur(telemetry.Trace, telemetryAModifier.Tours);
            }
		}

		private static Trace CreerTrace(Trace traceACreer)
		{
			traceACreer.Time = DateTime.Now;
			return traceACreer;
        }

		private static Tour CreerNouveauTour(int? numTourPrecedent = null)
		{
			return new()
			{
				NumTour = numTourPrecedent ?? 1,
				Secteurs = new() { CreerSecteur(numSecteurACreer: 1) }
            };
		}

		private static Tour? ObtenirDernierTour(List<Tour> tours)
		{
			return tours.OrderByDescending(x => x.NumTour).FirstOrDefault();
        }


        private static Secteur? ObtenirDernieSecteur(List<Secteur> secteurs)
        {
            return secteurs.OrderByDescending(x => x.NumSecteur).FirstOrDefault();
        }

        private static string CalculerTemps(DateTime tempsAComparer)
		{
            TimeSpan diff = DateTime.Now - tempsAComparer;

            int minutes = (int)diff.TotalMinutes;
            int seconds = (int)diff.TotalSeconds % 60;
            int milliseconds = diff.Milliseconds;

            Console.WriteLine($"Temps secteur : {string.Format("{0}:{1:00}.{2:000}", minutes, seconds, milliseconds)}");

            return string.Format("{0}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }

		private void VerifierNouveauSecteur(Trace trace, List<Tour> tours)
        {
            var dernierTour = ObtenirDernierTour(tours);
            ArgumentNullException.ThrowIfNull(dernierTour);

            var dernierSecteur = ObtenirDernieSecteur(dernierTour.Secteurs);
            ArgumentNullException.ThrowIfNull(dernierSecteur);

            var s1 = _zones.First(x => x.Secteur == 1);
            var s2 = _zones.First(x => x.Secteur == 2);
            var s3 = _zones.First(x => x.Secteur == 3); // ligne d'arrivée

            // à quel secteur nous en sommes ?
            switch (dernierSecteur.NumSecteur)
            {
                case 1:
                    if (EstSecteurDepasse(trace, s1))
                    {
                        dernierSecteur.Temps = CalculerTemps(tempsAComparer: dernierSecteur.DatePassage);
                        dernierTour.Secteurs.Add(CreerSecteur(numSecteurACreer: 2));
                    }
                    break;
                case 2:
                    if (EstSecteurDepasse(trace, s2))
                    {
                        dernierSecteur.Temps = CalculerTemps(tempsAComparer: dernierSecteur.DatePassage);
                        dernierTour.Secteurs.Add(CreerSecteur(numSecteurACreer: 3));
                    }
                    break;
                case 3:
                    if (EstSecteurDepasse(trace, s3))
                    {
                        dernierSecteur.Temps = CalculerTemps(tempsAComparer: dernierSecteur.DatePassage);
                        dernierTour.Temps = CalculerTempsAuTour(dernierTour.Secteurs);
                        tours.Add(CreerNouveauTour(dernierTour.NumTour));
                    }
                    break;
                default:
                    break;
            }
        }

        private static string CalculerTempsAuTour(List<Secteur> secteurs)
        {
            TimeSpan totalTime = TimeSpan.Zero;

            foreach (var temps in secteurs.Select(x => x.Temps))
            {
                totalTime += TimeSpan.ParseExact(temps, "m':'ss'.'fff", CultureInfo.InvariantCulture);
            }

            Console.WriteLine($"Temps calculé: {string.Format("{0}:{1:00}.{2:000}", (int)totalTime.TotalMinutes, totalTime.Seconds, totalTime.Milliseconds)}");

            return string.Format("{0}:{1:00}.{2:000}", (int)totalTime.TotalMinutes, totalTime.Seconds, totalTime.Milliseconds);
        }

        private static bool EstSecteurDepasse(Trace trace, Zone zone)
        {
            if (trace.Longitude >= zone.Coord.Y[0] && trace.Longitude <= zone.Coord.Y[1])
            {
                if ((zone.Sens == 1 && trace.Latitude >= zone.Coord.X) || (zone.Sens == -1 && trace.Latitude <= zone.Coord.X))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
                   
        }

        private static Secteur CreerSecteur(int numSecteurACreer)
		{
			return new()
			{
				DatePassage = DateTime.Now,
				NumSecteur = numSecteurACreer
			};
        }

		public async ValueTask DisposeAsync()
		{
			if (hubConnection is not null)
			{
				await hubConnection.DisposeAsync();
			}
		}
	}
}

