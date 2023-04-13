using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Jyben.Racing.Dashboard.Server.Services.Impl
{
	public class TelemetrieService : ITelemetrieService
	{

        private readonly IMongoCollection<Telemetrie> _telemetriesCollection;

        public TelemetrieService(
            IOptions<RacingDatabaseSettings> racingSettingsDatabase)
        {
            var mongoClient = new MongoClient(
                racingSettingsDatabase.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                racingSettingsDatabase.Value.DatabaseName);

            _telemetriesCollection = mongoDatabase.GetCollection<Telemetrie>(
                racingSettingsDatabase.Value.TelemetriesCollectionName);
        }

        public async Task<List<Telemetrie>> ObtenirAsync() =>
            await _telemetriesCollection.Find(_ => true).ToListAsync();

        public async Task<Telemetrie?> ObtenirParNomAsync(string nom) =>
            await _telemetriesCollection.Find(x => x.Nom == nom).FirstOrDefaultAsync();

        public async Task CreerAsync(Telemetrie nouvelleTelemetry) =>
            await _telemetriesCollection.InsertOneAsync(nouvelleTelemetry);

        public async Task MettreAJourAsync(string id, Telemetrie telemetryAJour) =>
            await _telemetriesCollection.ReplaceOneAsync(x => x.Id == id, telemetryAJour);

        public async Task SupprimerAsync(string id) =>
            await _telemetriesCollection.DeleteOneAsync(x => x.Id == id);
    }
}

