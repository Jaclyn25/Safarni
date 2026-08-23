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
    private readonly IGeoapifyService _geoapify;

    public TravelLocationsController(
        ITravelLocationCatalog catalog,
        IFlightAirportService airports,
        IGeoapifyService geoapify)
    {
        _catalog = catalog;
        _airports = airports;
        _geoapify = geoapify;
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> SuggestDestinations([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return Ok(ApiResponse<IReadOnlyList<DestinationSuggestion>>.SuccessResponse([], "Type at least two characters.", StatusCodes.Status200OK));

        var result = await _geoapify.SuggestDestinationsAsync(query, cancellationToken);
        if (result.IsFailure)
            return StatusCode(StatusCodes.Status502BadGateway,
                ApiResponse<IReadOnlyList<DestinationSuggestion>>.FailureResponse(
                    [new ApiError(result.Error.Code, "Destination suggestions are temporarily unavailable.")],
                    "Destination lookup failed.",
                    StatusCodes.Status502BadGateway));

        return Ok(ApiResponse<IReadOnlyList<DestinationSuggestion>>.SuccessResponse(
            result.Value!, "Destination suggestions loaded successfully.", StatusCodes.Status200OK));
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
