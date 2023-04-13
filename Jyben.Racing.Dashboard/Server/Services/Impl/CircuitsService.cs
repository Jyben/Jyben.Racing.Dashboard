using System.Text.Json;
using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.Extensions.FileProviders;

namespace Jyben.Racing.Dashboard.Server.Services.Impl
{
	public class CircuitsService : ICircuitsService
	{
        private IWebHostEnvironment _webHostEnvironment;

        public CircuitsService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public CircuitsDto LireDonneesCircuits()
		{
            var provider = _webHostEnvironment.ContentRootFileProvider;
            var fileInfo = provider.GetFileInfo(Path.Combine(_webHostEnvironment.ContentRootPath, "/circuits.json"));
            using var streamReader = new StreamReader(fileInfo.CreateReadStream());
            var content = streamReader.ReadToEnd();

            ArgumentNullException.ThrowIfNull(content);

            var json =  JsonSerializer.Deserialize<CircuitsDto>(content);

            ArgumentNullException.ThrowIfNull(json);

            return json;
        }
    }
}

