using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;
using Jyben.Racing.Dashboard.Client.Helpers;
using Jyben.Racing.Dashboard.Shared.Enums;
using Jyben.Racing.Dashboard.Shared.Helpers;
using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using SharpCompress.Common;

namespace Jyben.Racing.Dashboard.Client.Features.Dashboard
{
	public partial class DashboardPage : IAsyncDisposable
	{
		[Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] public HttpClient HttpClient { get; set; }

        private HubConnection? _hubConnection;
        private Canvas2DContext _context;
        protected BECanvasComponent _canvasReference;
        private GpsToPixelConverter _gpsToPixelConverter;
        private Circuit _circuit;
        private bool _isDisposed;
        private List<Pilote> _pilotes = new() { new Pilote() { Nom = "Sélectionner le pilote" } };
        private Pilote _pilote = new();
        private string _etatTelemetrie = "Démarrer";
        private string _classBtn = "btn-primary";
        private Telemetrie? _telemetrie;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_canvasReference != null)
            {
                _context = await _canvasReference.CreateCanvas2DAsync();
            }
        }

        /// <summary>
        /// Lecture des données via l'api et récéption des données via les events SignalR.
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
		{
            try
            {
                var result = await HttpClient.GetFromJsonAsync<CircuitsDto>("api/cirtcuits");

                ArgumentNullException.ThrowIfNull(result);

                _circuit = result.Circuits.First(x => x.Nom == "cik");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Une erreur est survenue lors de la désérialisation du fichier JSON : {0}", ex.Message);
            }

            try
            {
                var pilotes = await HttpClient.GetFromJsonAsync<List<Pilote>>("api/pilotes");

                if (pilotes is not null && pilotes.Any())
                {
                    _pilotes = pilotes;
                    _pilote = _pilotes.First();
                    await LireTelemetrie();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            _hubConnection = new HubConnectionBuilder()
				.WithUrl(NavigationManager.ToAbsoluteUri("/signalr/telemetry"))
				.Build();

            // réceptionne la télémétrie des pilotes
            _hubConnection.On("RecevoirTelemetry", (Func<string, Task>)(async (json) =>
            {
                await RecevoirTelemetrie(json);
            }));

            // réceptionne les noms et ids des pilotes
            _hubConnection.On("RecevoirPilote", (Action<string>)((piloteJson) =>
            {
                RecevoirPilote(piloteJson);
            }));

            // réceptionne l'information que le pilote est dans un nouveau tour
            _hubConnection.On("RecevoirInfoNouveauTour", (Action)(async () =>
            {
                // nettoie les traces affichées
                await _context.ClearRectAsync(0, 0, _circuit.Canevas.Width, _circuit.Canevas.Height);
            }));

            await _hubConnection.StartAsync();

            _gpsToPixelConverter = new(_circuit.Canevas.Width,
                _circuit.Canevas.Height,
                _circuit.Canevas.TopLeft.Longitude,
                _circuit.Canevas.TopLeft.Latitude,
                _circuit.Canevas.BottomRight.Longitude,
                _circuit.Canevas.BottomRight.Latitude);
        }

        /// <summary>
        /// Est appelé lorsque l'utilisateur change de pilote dans la select box.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task PiloteMisAJour(ChangeEventArgs e)
        {
            if (e.Value is null) return;

            var pilote = _pilotes.FirstOrDefault(x => x.Id == e.Value.ToString());

            if (pilote is null) return;

            _pilote = pilote;

            await LireTelemetrie();
        }

        /// <summary>
        /// Met à jour la liste des pilotes lorsqu'un pilote est connecté via SignalR.
        /// </summary>
        /// <param name="piloteJson">Le json content les infos du pilote.</param>
        private void RecevoirPilote(string piloteJson)
        {
            Console.WriteLine(piloteJson);

            var pilote = JsonSerializer.Deserialize<Pilote>(piloteJson);
            ArgumentNullException.ThrowIfNull(pilote);

            if (_pilotes.Any(x => x.Nom == pilote.Nom))
            {
                _pilotes.RemoveAt(_pilotes.FindIndex(x => x.Nom == pilote.Nom));
            }

            _pilotes.Add(pilote);
            StateHasChanged();
        }

        /// <summary>
        /// Gère l'affichage de la télémétrie reçue.
        /// </summary>
        /// <param name="telemetrieJson">Le json de la télémtrie reçue</param>
        /// <returns></returns>
        private async Task RecevoirTelemetrie(string telemetrieJson)
        {
            await LireTelemetrie();

            Console.WriteLine(telemetrieJson);

            try
            {
                var telemetrie = JsonSerializer.Deserialize<Telemetrie>(telemetrieJson);
                ArgumentNullException.ThrowIfNull(telemetrie);

                var dernierTour = TelemetrieHelper.ObtenirDernierTour(telemetrie.Tours);
                ArgumentNullException.ThrowIfNull(dernierTour);

                if (dernierTour.Traces != null && dernierTour.Traces.Count > 1)
                {
                    await DessinerSurLeCanevas(dernierTour.Traces);
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Lire la télémétrie complète d'un pilote pour l'afficher dans le tableau des temps.
        /// </summary>
        /// <returns></returns>
        private async Task LireTelemetrie()
        {
            if (_pilote is null || string.IsNullOrEmpty(_pilote.Nom)) return;

            Telemetrie? telemetrie = null;
            try
            {
                telemetrie = await HttpClient.GetFromJsonAsync<Telemetrie?>($"api/pilotes/{_pilote.Nom}/telemetries");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            // on vérifie que le pilote sélectionné est bien celui en course avant de mettre à jour le tableau
            if (telemetrie is not null && _pilote.Nom == telemetrie.Nom)
            {
                _telemetrie = telemetrie;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Dessine la trace du pilote.
        /// </summary>
        /// <param name="traces">Les coordonnées GPS du pilote.</param>
        /// <returns></returns>
        private async Task DessinerSurLeCanevas(List<Trace> traces)
        {
            var trace = traces.OrderByDescending(x => x.Time).First();
            var tracePrecedente = traces.OrderByDescending(x => x.Time).Skip(1).Take(1).First();

            // dessine la ligne en reliant chaque paire de coordonnées
            if (_context != null && traces.Count >= 2)
            {
                await _context.BeginPathAsync();

                // converti en les coordonnées GPS en pixels
                var point = _gpsToPixelConverter.Convert(traces[^2].Longitude, traces[^2].Latitude);
                await _context.MoveToAsync(point.X, point.Y);
                point = _gpsToPixelConverter.Convert(traces[^1].Longitude, traces[^1].Latitude);
                await _context.LineToAsync(point.X, point.Y);

                // personnalise l'apparence de la ligne
                var couleur = "";
                couleur = DetermineCouleurAccelerationFreinage(traces[^2].Vitesse, traces[^1].Vitesse);

                await _context.SetStrokeStyleAsync(couleur);
                await _context.SetLineWidthAsync(3);
                await _context.StrokeAsync();
            }
        }

        /// <summary>
        /// Détermine la couleur à partir de la vitesse actuelle et de la vitesse précédente.
        /// </summary>
        /// <param name="vitessePrecedente"></param>
        /// <param name="vitesseActuelle"></param>
        /// <returns></returns>
        public static string DetermineCouleurAccelerationFreinage(double vitessePrecedente, double vitesseActuelle)
        {
            double acceleration = (vitesseActuelle - vitessePrecedente);
            string couleur = "#FFFFFF"; // Blanc pour la stabilisation

            if (acceleration >= 5)
            {
                // Accélération forte
                couleur = "#008000"; // Vert foncé
            }
            else if (acceleration >= 3)
            {
                // Accélération moyenne
                couleur = "#00FF00"; // Vert clair
            }
            else if (acceleration <= -5)
            {
                // Freinage fort
                couleur = "#FF0000"; // Rouge foncé
            }
            else if (acceleration <= -3)
            {
                // Freinage moyen
                couleur = "#FFA500"; // Orange
            }
            else
            {
                // La vitesse est stabilisée
                couleur = "#FFFFFF";
            }

            return couleur;
        }

        /// <summary>
        /// Permet d'activer ou de désactiver l'envoi de données d'un pilote à distance.
        /// </summary>
        /// <returns></returns>
        private async Task BasculerTelemetrie()
        {
            if (!string.IsNullOrEmpty(_pilote.Id))
            {
                var pilote = _pilotes.First(x => x.Id == _pilote.Id);
                await _hubConnection!.InvokeAsync("BasculerTelemetrie", pilote.Id, pilote.SignalrId, !pilote.EstTelemetrieActivee);
                pilote.EstTelemetrieActivee = !pilote.EstTelemetrieActivee;
                if (pilote.EstTelemetrieActivee)
                {
                    _etatTelemetrie = "Arrêter";
                    _classBtn = "btn-danger";
                }
                else
                {
                    _etatTelemetrie = "Démarrer";
                    _classBtn = "btn-primary";
                }
            }
        }

        protected virtual async Task Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                if (_hubConnection is not null)
                {
                    await _hubConnection.DisposeAsync();
                }
                await DisposeAsync();
            }

            _isDisposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

