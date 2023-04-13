using System.Text.Json;
using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.Extensions.FileProviders;

namespace Jyben.Racing.Dashboard.Server.Services
{
	public interface ICircuitsService
	{
        CircuitsDto LireDonneesCircuits();
    }
}

