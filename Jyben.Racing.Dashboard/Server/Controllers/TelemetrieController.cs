using Microsoft.AspNetCore.Mvc;
using Jyben.Racing.Dashboard.Shared.Models;
using Jyben.Racing.Dashboard.Server.Services;

namespace Jyben.Racing.Dashboard.Server.Controllers;

[ApiController]
[Route("api")]
public class TelemtrieController : ControllerBase
{
    private readonly ILogger<TelemtrieController> _logger;
    private readonly ICircuitsService _circuitsService;
    private readonly ITelemetrieService _telemetrieService;
    private readonly IPiloteService _piloteService;

    public TelemtrieController(ILogger<TelemtrieController> logger, ICircuitsService circuitsService, ITelemetrieService telemetrieService, IPiloteService piloteService)
    {
        _logger = logger;
        _circuitsService = circuitsService;
        _telemetrieService = telemetrieService;
        _piloteService = piloteService;
    }

    [HttpGet]
    [Route("cirtcuits")]
    public CircuitsDto GetCircuits()
    {
        return _circuitsService.LireDonneesCircuits();
    }

    [HttpGet]
    [Route("pilotes/{nomPilote}/telemetries")]
    public async Task<ActionResult<Telemetrie>> GetTelemetriesParPilote(string nomPilote)
    {
        var telemetrie = await _telemetrieService.ObtenirParNomAsync(nomPilote);

        if (telemetrie is null)
        {
            return NotFound();
        }

        return telemetrie;
    }

    [HttpGet]
    [Route("telemetries")]
    public async Task<ActionResult<List<Telemetrie>>> GetTelemetries()
    {
        var telemetries = await _telemetrieService.ObtenirAsync();

        if (telemetries is null)
        {
            return NotFound();
        }

        return telemetries;
    }

    [HttpGet]
    [Route("pilotes")]
    public async Task<ActionResult<List<Pilote>>> GetPilotes()
    {
        var pilotes = await _piloteService.ObtenirAsync();

        if (pilotes is null || !pilotes.Any())
        {
            return NotFound();
        }

        return pilotes;
    }
}

