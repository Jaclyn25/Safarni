namespace Safarni.Domain.Entities;

public enum ItinerarySource
{
    Ai,
    Manual
}

public enum ItineraryStatus
{
    Generating,
    Completed,
    Failed
}

public enum ItineraryItemCategory
{
    Activity,
    Meal,
    Transport,
    Accommodation,
    Other
}
