using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iana.Timezone.MathExt
{
    public struct GeoCoordinate
    {
        public double Latitude;
        public double Longitude;

        public GeoCoordinate(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
        }
    }
}
