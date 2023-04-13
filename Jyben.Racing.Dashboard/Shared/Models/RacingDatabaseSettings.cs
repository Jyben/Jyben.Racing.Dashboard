namespace Jyben.Racing.Dashboard.Shared.Models
{
	public class RacingDatabaseSettings
	{
        public string ConnectionString { get; set; } = null!;

        public string DatabaseName { get; set; } = null!;

        public string TelemetriesCollectionName { get; set; } = null!;

        public string PilotesCollectionName { get; set; } = null!;
    }
}

