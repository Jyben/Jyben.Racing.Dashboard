using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Jyben.Racing.Dashboard.Shared.Models
{
	public class Pilote
	{
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string SignalrId { get; set; }

		public string Nom { get; set; }

        public bool EstTelemetrieActivee { get; set; }
    }
}

