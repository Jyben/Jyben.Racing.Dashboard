using System;
using Microsoft.AspNetCore.SignalR;

namespace Jyben.Racing.Dashboard.Server.Hubs
{
	public class TelemetryHub : Hub
	{
		public TelemetryHub()
		{
		}

		public async Task EnvoyerTelemetry(int userId, string json)
		{
			Console.WriteLine("reçu");
			await Clients.All.SendAsync("RecevoirTelemetry", userId, json);
		}
	}
}

