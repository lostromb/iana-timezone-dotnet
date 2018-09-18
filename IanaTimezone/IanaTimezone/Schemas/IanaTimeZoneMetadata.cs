using Iana.Timezone.MathExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iana.Timezone.Schemas
{
    /// <summary>
    /// Schema for entries in the IANA zone1970.tab file
    /// </summary>
    internal class IanaTimeZoneMetadata
    {
        public ISet<string> Countries { get; set; }
        public GeoCoordinate PrincipalCoordinate { get; set; }
        public string ZoneName { get; set; }
        public string Comment { get; set; }
    }
}
