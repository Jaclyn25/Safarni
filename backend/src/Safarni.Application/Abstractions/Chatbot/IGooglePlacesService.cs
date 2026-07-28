using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.Models.GooglePlaces;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IGooglePlacesService
{
    Task<List<PlaceDetails>> TextSearchAsync(string query, CancellationToken cancellationToken = default, string languageCode = "en");
    Task<List<PlaceDetails>> NearbySearchAsync(string location, string type = "", int radius = 5000, CancellationToken cancellationToken = default);
    Task<PlaceDetails?> GetPlaceDetailsAsync(string placeId, CancellationToken cancellationToken = default);
    string GetPhotoUrl(string photoReference, int maxWidth = 400);
}
