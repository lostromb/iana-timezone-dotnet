using System;
using System.Collections.Generic;
using System.Text;

namespace Iana.Timezone.Schemas
{
    /// <summary>
    /// Sticks together a time zone rule instance with a specific time which that rule takes effect
    /// </summary>
    internal class DstRuleAndEffectiveTime : IComparable<DstRuleAndEffectiveTime>
    {
        public DateTimeOffset RuleEffectiveTime;
        public IanaTimeZoneRule Rule;

        public DstRuleAndEffectiveTime(DateTimeOffset effectiveTime, IanaTimeZoneRule rule)
        {
            RuleEffectiveTime = effectiveTime;
            Rule = rule;
        }

        public int CompareTo(DstRuleAndEffectiveTime other)
        {
            return this.RuleEffectiveTime.CompareTo(other.RuleEffectiveTime);
        }

        public override string ToString()
        {
            return RuleEffectiveTime.ToString("yyyy-MM-ddTHH:mm:ss zzz") + " SAVE " + Rule.Save.ToString();
        }
    }
}
