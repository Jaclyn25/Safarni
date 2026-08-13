using System;
using System.Collections.Generic;
using System.Text;

namespace Safarni.Domain.Entities
{
    public class SnapshotVersion
    {
        public Guid Id { get; set; }
        public Guid SnapshotId { get; set; }
        public int VersionNumber { get; set; }

        /// <summary>Deep-copied itinerary/places/dates at publish time. Never mutated after creation.</summary>
        public string ItineraryJson { get; set; } = "{}";

        public bool CostShown { get; set; }
        public decimal? CostAmount { get; set; }

        /// <summary>Comma- or JSON-encoded list of included photo IDs.</summary>
        public string IncludedPhotoIdsJson { get; set; } = "[]";

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ViewsCount { get; set; } = 0;
        public int LikesCount { get; set; } = 0;
        public int SavesCount { get; set; } = 0;
        public int CopiesCount { get; set; } = 0;
        public int RatingsCount { get; set; } = 0;
        public double AverageRating { get; set; } = 0.0;

        public PublicTripSnapshot Snapshot { get; set; } = null!;
        public ICollection<TripInteraction> Interactions { get; set; } = new List<TripInteraction>();
        public ICollection<TripRating> Ratings { get; set; } = new List<TripRating>();
    }
}