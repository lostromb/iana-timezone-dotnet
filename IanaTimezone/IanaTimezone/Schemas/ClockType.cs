using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iana.Timezone.Schemas
{
    internal enum ClockType
    {
        /// <summary>
        /// Clock type is unknown
        /// </summary>
        Unspecified,

        /// <summary>
        /// The associated time is measured according to the global UTC clock.
        /// Specified by "u", "z", or "g" suffix in IANA
        /// </summary>
        Universal,

        /// <summary>
        /// The associated time is measured according to the current local time after factoring in daylight savings, etc.
        /// Specified by "w" suffix in IANA
        /// </summary>
        LocalWallClock,

        /// <summary>
        /// The associated time is measured according to the current local time WITHOUT factoring in daylight savings or other rules.
        /// Specified by "s" suffix in IANA
        /// </summary>
        LocalStandardTime
    }
}
