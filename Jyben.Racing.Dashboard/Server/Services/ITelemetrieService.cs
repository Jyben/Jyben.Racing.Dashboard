using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Jyben.Racing.Dashboard.Server.Services
{
	public interface ITelemetrieService
	{
        Task<List<Telemetrie>> ObtenirAsync();

        Task<Telemetrie?> ObtenirParNomAsync(string nom);

        Task CreerAsync(Telemetrie nouvelleTelemetry);

        Task MettreAJourAsync(string id, Telemetrie telemetryAJour);

        Task SupprimerAsync(string id);
    }
}

