using Iana.Timezone.MathExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iana.Timezone
{
    /// <summary>
    /// The result of a query for the local time in a specific location
    /// </summary>
    public class TimeZoneQueryResult
    {
        /// <summary>
        /// The local time at this location
        /// </summary>
        public DateTimeOffset LocalTime;

        /// <summary>
        /// The base offset from GMT / UTC time
        /// </summary>
        public TimeSpan GmtOffset;

        /// <summary>
        /// Any additional offset caused by this location's observance of daylight savings' time. Usually either 0 or 1 hours
        /// </summary>
        public TimeSpan DstOffset;

        /// <summary>
        /// The IANA name of this time zone
        /// </summary>
        public string TimeZoneName;

        /// <summary>
        /// The acronym for this timezone e.g. EDT
        /// </summary>
        public string TimeZoneAbbreviation;
        
        /// <summary>
        /// The exact coordinate of this query
        /// </summary>
        public GeoCoordinate QueryCoordinate;

        /// <summary>
        /// The coordinate used as the basis of timezone calculation; often a nearby city or populated area for which the timezone is named
        /// </summary>
        public GeoCoordinate TimeZoneBaseCoordinate;
    }
}
