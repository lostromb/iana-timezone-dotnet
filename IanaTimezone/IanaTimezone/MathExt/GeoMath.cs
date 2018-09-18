using System;
using System.Collections.Generic;
using System.Text;

namespace Iana.Timezone.MathExt
{
    public static class GeoMath
    {
        /// <summary>
        /// Calculates the distance in kilometers between two geo coordinates
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <returns></returns>
        public static double CalculateGeoDistance(GeoCoordinate pointA, GeoCoordinate pointB)
        {
            const double R = 6378.137; // Radius of the earth in km
            const double radians = Math.PI / 180;

            // Account for anti-meridian wrap
            if (pointB.Longitude - pointA.Longitude > 180)
            {
                pointA.Longitude += 360;
            }
            else if (pointA.Longitude - pointB.Longitude > 180)
            {
                pointB.Longitude += 360;
            }

            // ref http://stackoverflow.com/questions/27928/calculate-distance-between-two-latitude-longitude-points-haversine-formula
            double dLat = (pointB.Latitude - pointA.Latitude) * radians;
            double dLon = (pointB.Longitude - pointA.Longitude) * radians;
            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(pointA.Latitude * radians) * Math.Cos(pointB.Latitude * radians) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c; // Distance in km
            return d;
        }

        /// <summary>
        /// Accepts a Latitude coordinate and converts it into coordinates on the world map tile.
        /// (that is, a y-coordinate between 0 and 1, where 0 is the south pole, 1 is the north, 0.5 is the equator)
        /// </summary>
        /// <param name="lat"></param>
        /// <returns></returns>
        public static double MercatorProject(double lat)
        {
            double res = 2 * Math.PI * 6378137;
            double origShift = Math.PI * 6378137;
            double my = Math.Log(Math.Tan((90 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);
            my = my * origShift / 180;
            return (my + origShift) / res;
        }

        /// <summary>
        /// Accepts a double representing the "y-coordinate on the world map tile" (where 0.0 = the south pole and
        /// 1.0 equals the north pole), and converts it into a latitude that corresponds to the WGS84 Datum.
        /// </summary>
        /// <param name="yCoord"></param>
        /// <returns></returns>
        public static double InverseMercatorProject(double yCoord)
        {
            double res = 2 * Math.PI * 6378137;
            double origShift = Math.PI * 6378137;
            double my = yCoord * res - origShift;
            double lat = (my / origShift) * 180;
            lat = (180 / Math.PI) * (2 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);
            return lat;
        }
    }
}
