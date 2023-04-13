using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Jyben.Racing.Dashboard.Server.Services.Impl
{
	public class PiloteService : IPiloteService
	{

        private readonly IMongoCollection<Pilote> _pilotesCollection;

        public PiloteService(
            IOptions<RacingDatabaseSettings> racingSettingsDatabase)
        {
            var mongoClient = new MongoClient(
                racingSettingsDatabase.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                racingSettingsDatabase.Value.DatabaseName);

            _pilotesCollection = mongoDatabase.GetCollection<Pilote>(
                racingSettingsDatabase.Value.PilotesCollectionName);
        }

        public async Task<List<Pilote>> ObtenirAsync() =>
            await _pilotesCollection.Find(_ => true).ToListAsync();

        public async Task<Pilote?> ObtenirParIdAsync(string id) =>
            await _pilotesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<Pilote?> ObtenirParNomAsync(string nom) =>
            await _pilotesCollection.Find(x => x.Nom == nom).FirstOrDefaultAsync();

        public async Task CreerAsync(Pilote nouveauPilote) =>
            await _pilotesCollection.InsertOneAsync(nouveauPilote);

        public async Task MettreAJourAsync(string id, Pilote piloteAJour) =>
            await _pilotesCollection.ReplaceOneAsync(x => x.Id == id, piloteAJour);

        public async Task SupprimerAsync(string id) =>
            await _pilotesCollection.DeleteOneAsync(x => x.Id == id);
    }
}

