using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Iana.Timezone.Schemas
{
    /// <summary>
    /// Represents a "Rule" definition in the IANA tz database files. For example:
    /// Rule    Libya   1986    only    -   Apr 4   0:00    1:00    S
    /// </summary>
    internal class IanaTimeZoneRule
    {
        private static readonly Regex _atMatcher = new Regex("(?:(\\d{1,2}):)?(\\d{1,2}):(\\d{1,2})(.*)");
        private static readonly Regex _saveMatcher = new Regex("(-)?(?:(\\d{1,2}):)?(\\d{1,2})");

        /// <summary>
        /// Name of this rule. All characters are in the set [a-zA-Z_-]
        /// </summary>
        private string _name;

        /// <summary>
        /// Gives the first year in which the rule applies.  Any signed
        /// integer year can be supplied; the proleptic Gregorian calendar
        /// is assumed, with year 0 preceding year 1. The word "minimum" (or
        /// an abbreviation) means the indefinite past. The word "maximum"
        /// (or an abbreviation) means the indefinite future. Rules can
        /// describe times that are not representable as time values, with
        /// the unrepresentable times ignored; this allows rules to be
        /// portable among hosts with differing time value types.
        /// </summary>
        private int _fromYear;

        /// <summary>
        /// Gives the final year in which the rule applies. In addition to
        /// minimum and maximum (as above), the word "only" (or an
        /// abbreviation) may be used to repeat the value of the FROM
        /// field.
        /// </summary>
        private int _toYear;

        /// <summary>
        /// Should always be "-"
        /// </summary>
        private string _type;

        /// <summary>
        /// A 3-char month name when this rule takes effect (converted to an integer where Jan == 1)
        /// </summary>
        private int _in;

        /// <summary>
        /// 
        /// </summary>
        private string _onRule;
        private IanaTimeZoneRuleType _onRuleType;
        private int _onDayOfMonth;

        /// <summary>
        /// The "On" day-of-week in ISO form; mon = 1, sun = 0
        /// </summary>
        private int _onDayOfWeek;

        /// <summary>
        /// A time of day expression in the form "hh:mm:ssZ" where Z is an optional "w", "s", "u", "g", or "z".
        /// W = "According to local wallclock time"
        /// S = "According to local standard time"
        /// U/G/Z = "According to universal time"
        /// </summary>
        private TimeSpan _at;

        /// <summary>
        /// The clock time at which the "at" takes effect
        /// </summary>
        private ClockType _atModifier;

        /// <summary>
        /// A time span value in the form "h:mm" indicating the amount of daylight savings offset
        /// </summary>
        private TimeSpan _save;

        /// <summary>
        /// A modifier string to pass into Zone.Format while this rule is in effect, or "-" to indicate no value
        /// </summary>
        private string _letter;

        public IanaTimeZoneRule(string name, string from, string to, string type, string @in, string on, string at, string save, string letter)
        {
            _name = name;
            if (string.Equals("min", to, StringComparison.OrdinalIgnoreCase))
            {
                _fromYear = 1;
            }
            else if (!int.TryParse(from, out _fromYear))
            {
                throw new ArgumentException("Could not parse FROM year \"" + from + "\" in rule " + _name);
            }

            if (string.Equals("only", to, StringComparison.OrdinalIgnoreCase))
            {
                _toYear = _fromYear;
            }
            else if (string.Equals("max", to, StringComparison.OrdinalIgnoreCase))
            {
                _toYear = 9999;
            }
            else if (!int.TryParse(to, out _toYear))
            {
                throw new ArgumentException("Could not parse TO year \"" + to + "\" in rule " + _name);
            }

            _type = type;
            _in = TimeZoneHelpers.ParseMonthName(@in);

            on = on.Trim();
            _onRule = on;
            // "On" is EITHER:
            // - A numerical day of month
            // - An expression in the form "Sun>=15"
            // - An expression in the form "lastSat"
            if (on.Contains(">="))
            {
                _onDayOfWeek = TimeZoneHelpers.ParseDayOfWeekName(on.Substring(0, 3));
                _onDayOfMonth = int.Parse(on.Substring(5));
                _onRuleType = IanaTimeZoneRuleType.DayOfWeekOnOrAfter;
            }
            else if (on.StartsWith("last"))
            {
                _onDayOfMonth = -1;
                _onDayOfWeek = TimeZoneHelpers.ParseDayOfWeekName(on.Substring(4));
                _onRuleType = IanaTimeZoneRuleType.LastDayOfWeek;
            }
            else if (int.TryParse(on, out _onDayOfMonth))
            {
                _onRuleType = IanaTimeZoneRuleType.DayOfMonth;
            }

            Match atMatch = _atMatcher.Match(at);
            if (!atMatch.Success)
            {
                throw new ArgumentException("Could not parse AT \"" + at + "\" in rule " + _name);
            }

            _at = TimeSpan.Zero;
            if (atMatch.Groups[1].Success)
            {
                _at = _at.Add(TimeSpan.FromHours(int.Parse(atMatch.Groups[1].Value)));
                _at = _at.Add(TimeSpan.FromMinutes(int.Parse(atMatch.Groups[2].Value)));
                _at = _at.Add(TimeSpan.FromSeconds(int.Parse(atMatch.Groups[3].Value)));
            }
            else
            {
                _at = _at.Add(TimeSpan.FromHours(int.Parse(atMatch.Groups[2].Value)));
                _at = _at.Add(TimeSpan.FromMinutes(int.Parse(atMatch.Groups[3].Value)));
            }

            if (atMatch.Groups[4].Success)
            {
                string mod = atMatch.Groups[4].Value;
                if (string.Equals("s", mod, StringComparison.OrdinalIgnoreCase))
                {
                    _atModifier = ClockType.LocalStandardTime;
                }
                else if (string.Equals("w", mod, StringComparison.OrdinalIgnoreCase))
                {
                    _atModifier = ClockType.LocalWallClock;
                }
                else if (string.Equals("u", mod, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("g", mod, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals("z", mod, StringComparison.OrdinalIgnoreCase))
                {
                    _atModifier = ClockType.Universal;
                }
                else if (string.IsNullOrEmpty(mod))
                {
                    _atModifier = ClockType.Unspecified;
                }
                else
                {
                    throw new ArgumentException("Could not parse AT modifier " + _at + " in rule " + _name);
                }
            }
            else
            {
                _atModifier = ClockType.Unspecified;
            }
            
            _save = TimeSpan.Zero;

            Match saveMatch = _saveMatcher.Match(save);
            if (!saveMatch.Success)
            {
                throw new ArgumentException("Could not parse SAVE \"" + save + "\" in rule " + _name);
            }
            if (saveMatch.Groups[2].Success)
            {
                _save = _save.Add(TimeSpan.FromHours(int.Parse(saveMatch.Groups[2].Value)));
                _save = _save.Add(TimeSpan.FromMinutes(int.Parse(saveMatch.Groups[3].Value)));
            }
            else
            {
                _save = _save.Add(TimeSpan.FromHours(int.Parse(saveMatch.Groups[3].Value)));
            }
            if (saveMatch.Groups[1].Success)
            {
                _save = TimeSpan.Zero - _save;
            }

            _letter = letter;
        }

        public TimeSpan Save
        {
            get
            {
                return _save;
            }
        }

        public ClockType AtModifier
        {
            get
            {
                return _atModifier;
            }
        }

        public string Letter
        {
            get
            {
                return _letter;
            }
        }

        public DateTimeOffset? GetRuleEffectiveTime(int year)
        {
            if (year < _fromYear || year > _toYear)
            {
                return null;
            }

            DateTimeOffset returnVal = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
            returnVal = returnVal.AddMonths(_in - 1);

            switch (_onRuleType)
            {
                case IanaTimeZoneRuleType.DayOfMonth:
                    returnVal = returnVal.AddDays(_onDayOfMonth - 1);
                    break;
                case IanaTimeZoneRuleType.DayOfWeekOnOrAfter:
                    returnVal = returnVal.AddDays(_onDayOfMonth - 1);
                    int daysNext = _onDayOfWeek - (int)returnVal.DayOfWeek;
                    if (daysNext < 0)
                    {
                        daysNext += 7;
                    }

                    // zoom to next __day
                    returnVal = returnVal.AddDays(daysNext);

                    break;
                case IanaTimeZoneRuleType.LastDayOfWeek:
                    returnVal = returnVal.AddMonths(1);
                    // zoom to previous __day
                    int daysPrev = (int)returnVal.DayOfWeek - _onDayOfWeek;
                    if (daysPrev <= 0)
                    {
                        daysPrev += 7;
                    }

                    // zoom to previous __day
                    returnVal = returnVal.AddDays(0 - daysPrev);
                    break;
            }

            returnVal = returnVal.Add(_at);
            return returnVal;
        }

        public override string ToString()
        {
            // name, string from, string to, string type, string @in, string on, string at, string save, string letter)
            return "RULE " + _name + " | FROM " + _fromYear + "-" + _toYear + " | IN " + _in + " | ON " + _onRule + " | AT " + _at + _atModifier + " | SAVE " + _save;
        }
    }
}
