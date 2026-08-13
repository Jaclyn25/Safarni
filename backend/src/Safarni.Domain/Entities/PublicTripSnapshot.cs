using System;
using System.Collections.Generic;
using System.Text;

namespace Safarni.Domain.Entities
{
    public class PublicTripSnapshot
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Trip Trip { get; set; } = null!;
        public ICollection<SnapshotVersion> Versions { get; set; } = new List<SnapshotVersion>();
    }
}