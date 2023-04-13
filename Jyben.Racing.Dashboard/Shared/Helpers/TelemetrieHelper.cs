using System;
using Jyben.Racing.Dashboard.Shared.Models;
using System.Globalization;

namespace Jyben.Racing.Dashboard.Shared.Helpers
{
	public class TelemetrieHelper
	{
		public TelemetrieHelper()
		{
		}

        /// <summary>
        /// Initialise une nouvelle trace avec la date actuelle.
        /// </summary>
        /// <param name="traceAInitialiser">La trace à initialiser.</param>
        /// <returns>La trace initialisée.</returns>
        public static Trace InitialiserUneTrace(Trace traceAInitialiser)
        {
            traceAInitialiser.Time = DateTime.UtcNow;
            return traceAInitialiser;
        }

        /// <summary>
        /// Créé un nouveau tour et créé le premier secteur.
        /// </summary>
        /// <param name="numeroDuNouveauTour">Le numéro du nouveau tour à créer.</param>
        /// <returns>Le tour créé.</returns>
        public static Tour CreerNouveauTour(int numeroDuNouveauTour, Trace? trace = null)
        {
            var tour = new Tour()
            {
                NumTour = numeroDuNouveauTour,
                Secteurs = new() { CreerSecteur(numeroDuSecteurACreer: 1) },
            };

            if (trace is not null)
            {
                tour.Traces = new() { InitialiserUneTrace(trace) };
            }

            return tour;
        }

        /// <summary>
        /// Obtient le dernier tour en triant par numéro de tour.
        /// </summary>
        /// <param name="tours">La liste des tours.</param>
        /// <returns>Le tour s'il est trouvé, sinon retourne une valeur null.</returns>
        public static Tour? ObtenirDernierTour(List<Tour> tours)
        {
            return tours.OrderByDescending(x => x.NumTour).FirstOrDefault();
        }

        /// <summary>
        /// Obtient le dernier secteur du tour en triant par numéro de secteur.
        /// </summary>
        /// <param name="secteurs">La liste des secteurs du tour.</param>
        /// <returns>Le secteur s'il est trouvé, sinon retourne une valeur null.</returns>
        public static Secteur? ObtenirDernieSecteur(List<Secteur> secteurs)
        {
            return secteurs.OrderByDescending(x => x.NumSecteur).FirstOrDefault();
        }

        /// <summary>
        /// Calcule le temps entre le temps donné et le temps actuel
        /// </summary>
        /// <param name="tempsAComparer"></param>
        /// <returns>Le temps calculé au format minutes:secondes:millisecondes</returns>
        public static string CalculerTemps(DateTime tempsAComparer)
        {
            TimeSpan diff = DateTime.UtcNow - tempsAComparer;

            int minutes = (int)diff.TotalMinutes;
            int seconds = (int)diff.TotalSeconds % 60;
            int milliseconds = diff.Milliseconds;

            return string.Format("{0}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }

        /// <summary>
        /// Calcule spécifique le temps au tour en fonctione de la liste des secteurs passés à la méthode. 
        /// </summary>
        /// <param name="secteurs">La liste des secteurs contenant tous les temps.</param>
        /// <returns>Le temps calculé au format minutes:secondes:millisecondes</returns>
        public static string CalculerTempsAuTour(List<Secteur> secteurs)
        {
            TimeSpan totalTime = TimeSpan.Zero;

            foreach (var temps in secteurs.Select(x => x.Temps))
            {
                totalTime += TimeSpan.ParseExact(temps, "m':'ss'.'fff", CultureInfo.InvariantCulture);
            }

            Console.WriteLine($"Temps calculé: {string.Format("{0}:{1:00}.{2:000}", (int)totalTime.TotalMinutes, totalTime.Seconds, totalTime.Milliseconds)}");

            return string.Format("{0}:{1:00}.{2:000}", (int)totalTime.TotalMinutes, totalTime.Seconds, totalTime.Milliseconds);
        }

        /// <summary>
        /// Vérifie si un secteur vient d'être dépassé en comparant les coordonnées GPS du pilote et les coordonées du secteur (zone). 
        /// </summary>
        /// <param name="trace">La trace GPS du pilote.</param>
        /// <param name="zone">La zone représentant un secteur.</param>
        /// <returns></returns>
        public static bool EstSecteurDepasse(Trace trace, Zone zone)
        {
            // compare la longitude de la trace avec les 2 coordonées Y du secteur du circuit
            bool longitudeValide = (trace.Longitude >= zone.Coord.Y[0] && trace.Longitude <= zone.Coord.Y[1]);

            // compare la latitude de la trace avec la coordonée X du secteur du circuit en fonctionne du sens
            bool latitudeValide = (zone.Sens == 1 && trace.Latitude >= zone.Coord.X) || (zone.Sens == -1 && trace.Latitude <= zone.Coord.X);

            // si les 2 tests sont valides, le pilote vient de dépasser un le secteur
            return longitudeValide && latitudeValide;
        }

        /// <summary>
        /// Créé un secteur avec une date de passage et son numéro.
        /// </summary>
        /// <param name="numeroDuSecteurACreer">Le numéro du secteur à créer.</param>
        /// <returns>Le nouveau secteur créé.</returns>
        public static Secteur CreerSecteur(int numeroDuSecteurACreer)
        {
            return new()
            {
                DatePassage = DateTime.UtcNow,
                NumSecteur = numeroDuSecteurACreer
            };
        }
    }
}

