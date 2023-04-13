using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Jyben.Racing.Dashboard.Server.Services
{
	public interface IPiloteService
	{
        Task<List<Pilote>> ObtenirAsync();

        Task<Pilote?> ObtenirParNomAsync(string nom);

        Task<Pilote?> ObtenirParIdAsync(string id);

        Task CreerAsync(Pilote nouveauPilote);

        Task MettreAJourAsync(string id, Pilote piloteAJour);

        Task SupprimerAsync(string id);
    }
}

