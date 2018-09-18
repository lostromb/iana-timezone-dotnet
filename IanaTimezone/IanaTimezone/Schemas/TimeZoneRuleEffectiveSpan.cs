using System;
using System.Collections.Generic;
using System.Text;

namespace Iana.Timezone.Schemas
{
    /// <summary>
    /// Represents the effective time for a daylight savings time rule and the data relating to its next transition
    /// </summary>
    public class TimeZoneRuleEffectiveSpan
    {
        /// <summary>
        /// The effective local time that the rule begins
        /// </summary>
        public DateTimeOffset RuleBoundaryBegin;

        /// <summary>
        /// The effective local time that the rule ends
        /// </summary>
        public DateTimeOffset RuleBoundaryEnd;

        /// <summary>
        /// The current GMT offset applied to this span
        /// </summary>
        public TimeSpan GmtOffset;

        /// <summary>
        /// The current DST offset applied to this span (on top of GMT - normally 0 or 1 hours)
        /// </summary>
        public TimeSpan DstOffset;

        /// <summary>
        /// The abbreviation that describes the clock while this rule takes effect (for example, "EST" vs. "EDT" for standard and daylight times respectively)
        /// </summary>
        public string TimeZoneAbbreviation;

        public override string ToString()
        {
            return RuleBoundaryBegin.ToString("yyyy-MM-ddTHH:mm:ss zzz") + " => " + RuleBoundaryEnd.ToString("yyyy-MM-ddTHH:mm:ss zzz") + " offset " + DstOffset.ToString();
        }
    }
}
