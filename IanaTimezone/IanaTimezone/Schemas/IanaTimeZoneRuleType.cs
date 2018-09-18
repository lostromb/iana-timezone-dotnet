using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iana.Timezone.Schemas
{
    internal enum IanaTimeZoneRuleType
    {
        /// <summary>
        /// "On" refers to a specific day of the month e.g. 15
        /// </summary>
        DayOfMonth,

        /// <summary>
        /// "On" refers to the first particular day of week on or after a particular boundary e.g. "Sun>=15"
        /// </summary>
        DayOfWeekOnOrAfter,

        /// <summary>
        /// "On" refers to the last day of the week that falls within a particular month e.g. "lastFri"
        /// </summary>
        LastDayOfWeek
    }
}
