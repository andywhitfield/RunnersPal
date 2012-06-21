using Massive;
using System;

namespace RunnersPal
{
    public class Route : DynamicModel
    {
        public static readonly char PublicRoute = 'P';
        public static readonly char PrivateRoute = 'M';
        public static readonly char SystemRoute = 'Z';
        public static readonly char DeletedRoute = 'D';

        public Route() : base(MassiveDB.ConnectionStringName, "Route", "Id") { }
    }

    public static class RouteExtensions
    {
        public static Distance Distance(this object route)
        {
            var dynRoute = route as dynamic;
            var distanceUnitInt = (int)dynRoute.DistanceUnits;
            var distanceUnit = DistanceUnits.Miles;
            if (Enum.IsDefined(typeof(DistanceUnits), distanceUnitInt))
                distanceUnit = (DistanceUnits)distanceUnitInt;

            return new Distance(dynRoute.Distance, distanceUnit);
        }
    }
}
