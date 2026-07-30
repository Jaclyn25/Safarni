using System;
using System.Collections.Generic;
using System.Text;

namespace Safarni.Application.Common.Errors
{
    public static class TripErrors
    {
        public static readonly Error NotFound = new(
            "Trip.NotFound",
            "The requested trip was not found.",
            ErrorType.NotFound);
    }
}
