using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Abstractions.Travel;

namespace Safarni.API.Controllers;

[ApiController]
[Route("api/travel-locations")]
public sealed class TravelLocationsController
    : ControllerBase
{
    private readonly ITravelLocationCatalog _catalog;
    private readonly IFlightAirportService _airports;

    public TravelLocationsController(
        ITravelLocationCatalog catalog,
        IFlightAirportService airports)
    {
        _catalog = catalog;
        _airports = airports;
    }

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(
        CancellationToken cancellationToken)
    {
        var countries =
            await _catalog.GetCountriesAsync(
                cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<TravelCountry>>
                .SuccessResponse(
                    countries,
                    "Countries loaded successfully.",
                    StatusCodes.Status200OK));
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities(
        [FromQuery] string country,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return BadRequest(
                ApiResponse<IReadOnlyList<string>>
                    .FailureResponse(
                        [
                            new ApiError(
                                "TravelLocations.CountryRequired",
                                "Choose a destination country first.")
                        ],
                        "A country is required.",
                        StatusCodes.Status400BadRequest));
        }

        var cities =
            await _catalog.GetCitiesAsync(
                country,
                cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<string>>
                .SuccessResponse(
                    cities,
                    "Cities loaded successfully.",
                    StatusCodes.Status200OK));
    }

    [HttpGet("airports")]
    public async Task<IActionResult> GetAirports(
        [FromQuery] string country,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return BadRequest();
        }

        var airports =
            await _airports.SuggestAirportsAsync(
                country,
                cancellationToken);

        return Ok(
            ApiResponse<
                IReadOnlyList<FlightAirportSuggestion>>
                .SuccessResponse(
                    airports,
                    "Departure airports loaded successfully.",
                    StatusCodes.Status200OK));
    }
}
