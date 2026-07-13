using Safarni.Domain.Entities;

namespace Safarni.Application.Features.VisaApplications;

internal static class VisaApplicationMapping
{
    public static VisaApplicationResponse ToResponse(this VisaApplication application)
    {
        return new VisaApplicationResponse(
            application.Id,
            application.UserId,
            application.DestinationCountry,
            application.TravelPurpose,
            application.PlannedTravelDate,
            application.VisaType,
            application.Status,
            application.CreatedAt,
            application.UpdatedAt);
    }
}
