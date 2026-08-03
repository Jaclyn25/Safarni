namespace Safarni.Application.Abstractions.Security;

public interface IItineraryLinkFactory
{
    string Create(Guid tripId);
}
