using System.Globalization;
using System.Text.Json;
using Jyben.Racing.Dashboard.Server.Services;
using Jyben.Racing.Dashboard.Shared.Helpers;
using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace Jyben.Racing.Dashboard.Server.Hubs
{
    /// <summary>
    /// Hub SignalR réceptionnant la télémétrie des pilotes.
    /// Lit la configuration du circuit, fait les appels à la BDD, applique les algorithmes de calcul des temps, des secteurs, etc., et envoie les données traitées au client. 
    /// </summary>
	public class TelemetryHub : Hub
	{
        private Circuit _circuit;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITelemetrieService _telemetrieService;
        private readonly ICircuitsService _circuitsService;
        private readonly IPiloteService _piloteService;

        public TelemetryHub(IWebHostEnvironment webHostEnvironment, ITelemetrieService telemetryService, ICircuitsService circuitsService, IPiloteService piloteService)
		{
            _webHostEnvironment = webHostEnvironment;
            _telemetrieService = telemetryService;
            _circuitsService = circuitsService;
            _piloteService = piloteService;
        }

        /// <summary>
        /// Méthode du hub SignalR qui réceptionne la connexion des pilotes.
        /// </summary>
        /// <param name="nomPilote">Le nom du pilote connecté.</param>
        /// <returns></returns>
        public async Task Connexion(string nomPilote)
        {
            var pilote = new Pilote()
            {
                SignalrId = Context.ConnectionId,
                Nom = nomPilote
            };

            var piloteEnBase = await _piloteService.ObtenirParNomAsync(nomPilote);

            if (piloteEnBase is null)
            {
                await _piloteService.CreerAsync(pilote);
            }
            else
            {
                pilote.Id = piloteEnBase.Id;
                // on met à jour pour l'id signalr
                await _piloteService.MettreAJourAsync(pilote.Id!, pilote);
            }

            var piloteJson = JsonSerializer.Serialize(pilote);

            await Clients.All.SendAsync("RecevoirPilote", piloteJson);
        }

        /// <summary>
        /// Méthode du hub SignalR qui réceptionne les bascules de la télémétrie d'un pilote.
        /// </summary>
        /// <param name="piloteId">L'id du pilote pour lequel il faut basculer la télémétrie.</param>
        /// <param name="signalrId">Le client id SignalR du pilote.</param>
        /// <param name="estTelemetrieActivee">L'état à changer de la télémétrie du pilote.</param>
        /// <returns></returns>
        public async Task BasculerTelemetrie(string piloteId, string signalrId, bool estTelemetrieActivee)
        {
            await Clients.Client(signalrId).SendAsync("BasculerTelemetrie", estTelemetrieActivee);

            var pilote = await _piloteService.ObtenirParIdAsync(piloteId);

            ArgumentNullException.ThrowIfNull(pilote);

            pilote.EstTelemetrieActivee = estTelemetrieActivee;

            await _piloteService.MettreAJourAsync(pilote.Id!, pilote);
        }

        /// <summary>
        /// Méthode du hub SignalR qui réceptionne la télémétrie des pilotes. 
        /// </summary>
        /// <param name="json">L'objet json de télémétrie.</param>
        /// <returns></returns>
		public async Task EnvoyerTelemetrie(string json)
		{
            try
            {
                var provider = _webHostEnvironment.ContentRootFileProvider;
                var fileInfo = provider.GetFileInfo(Path.Combine(_webHostEnvironment.ContentRootPath, "/circuits.json"));
                using var streamReader = new StreamReader(fileInfo.CreateReadStream());
                var content = streamReader.ReadToEnd();

                ArgumentNullException.ThrowIfNull(content);

                var circuits = JsonSerializer.Deserialize<CircuitsDto>(content);

                ArgumentNullException.ThrowIfNull(circuits);

                // TODO : paramétrer le choix du circuit
                _circuit = circuits.Circuits.First(x => x.Nom == "cik");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Une erreur est survenue lors de la désérialisation du fichier JSON : {0}", ex.Message);
            }

            var telemetryPilote = JsonSerializer.Deserialize<TelemetriePilote>(json);

            ArgumentNullException.ThrowIfNull(telemetryPilote);

            var telemetryAJour = await MettreAJourTelemetry(telemetryPilote);

            ArgumentNullException.ThrowIfNull(telemetryAJour);

            var tours = telemetryAJour.Tours.TakeLast(2);
            telemetryAJour.Tours = tours.ToList();

            var telemetrie = JsonSerializer.Serialize(telemetryAJour);

            await Clients.All.SendAsync("RecevoirTelemetry", telemetrie);
		}

        /// <summary>
        /// Calcul les tours et les secteurs et met à jour les données en base.
        /// </summary>
        /// <param name="telemetrie">La télémétrie reçue du pilote.</param>
        /// <returns></returns>
        private async Task<Telemetrie?> MettreAJourTelemetry(TelemetriePilote? telemetrie)
        {
            if (telemetrie == null) return null;

            // vérifie si la télémétrie du pilote existe déjà en base
            var telemetrieLue = await _telemetrieService.ObtenirParNomAsync(telemetrie.Nom);

            if (telemetrieLue is null)
            {
                // créée la télémétrie avec une nouvelle trace et un nouveau tour
                var telemetrieACreer = new Telemetrie()
                {
                    Nom = telemetrie.Nom,
                    Tours = new() { TelemetrieHelper.CreerNouveauTour(1, telemetrie.Trace) }
                };

                await _telemetrieService.CreerAsync(telemetrieACreer);

                ArgumentNullException.ThrowIfNull(telemetrieACreer.Id);

                return telemetrieACreer;
            }
            else
            {
                var dernierTour = TelemetrieHelper.ObtenirDernierTour(telemetrieLue.Tours);

                ArgumentNullException.ThrowIfNull(dernierTour);

                dernierTour.Traces ??= new();

                // ajout d'une nouvelle trace
                dernierTour.Traces.Add(TelemetrieHelper.InitialiserUneTrace(telemetrie.Trace));

                // gestion des secteurs
                await VerifierNouveauSecteur(telemetrie.Trace, telemetrieLue.Tours, telemetrieLue.Nom);

                await _telemetrieService.MettreAJourAsync(telemetrieLue.Id!, telemetrieLue);

                return telemetrieLue;
            }
        }

        /// <summary>
        /// Vérifie si le pilote a dépassé un secteur et calcul le temps au secteur.
        /// Créé également un nouveau tour si le secteur dépassé est le n°3, et calcul le temps au tour.
        /// </summary>
        /// <param name="trace">La trace envoyée par le pilote avec ses coordonées GPS.</param>
        /// <param name="tours">La liste des tours déjà effectués. Doit être initialisée avec au moins 1 tour.</param>
        /// <param name="nomPilote">Le nom du pilote correspond à la trace.</param>
        private async Task VerifierNouveauSecteur(Trace trace, List<Tour> tours, string nomPilote)
        {
            var dernierTour = TelemetrieHelper.ObtenirDernierTour(tours);
            ArgumentNullException.ThrowIfNull(dernierTour);

            var dernierSecteur = TelemetrieHelper.ObtenirDernieSecteur(dernierTour.Secteurs);
            ArgumentNullException.ThrowIfNull(dernierSecteur);

            var s1 = _circuit.Zone.First(x => x.Secteur == 1);
            var s2 = _circuit.Zone.First(x => x.Secteur == 2);
            var s3 = _circuit.Zone.First(x => x.Secteur == 3); // ligne d'arrivée

            switch (dernierSecteur.NumSecteur)
            {
                case 1:
                    if (TelemetrieHelper.EstSecteurDepasse(trace, s1))
                    {
                        dernierSecteur.Temps = TelemetrieHelper.CalculerTemps(tempsAComparer: dernierSecteur.DatePassage);
                        dernierTour.Secteurs.Add(TelemetrieHelper.CreerSecteur(numeroDuSecteurACreer: 2));
                    }
                    break;
                case 2:
                    if (TelemetrieHelper.EstSecteurDepasse(trace, s2))
                    {
                        dernierSecteur.Temps = TelemetrieHelper.CalculerTemps(tempsAComparer: dernierSecteur.DatePassage);
                        dernierTour.Secteurs.Add(TelemetrieHelper.CreerSecteur(numeroDuSecteurACreer: 3));
                    }
                    break;
                case 3:
                    if (TelemetrieHelper.EstSecteurDepasse(trace, s3))
                    {
                        dernierSecteur.Temps = TelemetrieHelper.CalculerTemps(tempsAComparer: dernierSecteur.DatePassage);
                        dernierTour.Temps = TelemetrieHelper.CalculerTempsAuTour(dernierTour.Secteurs);
                        tours.Add(TelemetrieHelper.CreerNouveauTour(dernierTour.NumTour + 1));

                        await Clients.All.SendAsync("RecevoirInfoNouveauTour");

                        // permet de notifier le pilote si le dernier tour était son meilleur temps
                        var meilleurTour = tours.OrderBy(x => x.Temps).First(x => x.Temps is not null);
                        if (meilleurTour.NumTour == dernierTour.NumTour)
                        {
                            var pilote = await _piloteService.ObtenirParNomAsync(nomPilote);
                            if (pilote is not null)
                            {
                                await Clients.Client(pilote.SignalrId).SendAsync("NotifierMeilleurTemps");
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}

