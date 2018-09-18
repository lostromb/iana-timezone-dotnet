using Iana.Timezone.MathExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iana.Timezone.Schemas
{
    internal struct TimeZonePoint
    {
        public GeoCoordinate Coords;

        public string TimeZoneId;

        public TimeZonePoint(GeoCoordinate coords, string zone)
        {
            Coords = coords;
            TimeZoneId = zone;
        }
    }
}
