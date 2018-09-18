using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Iana.Timezone.Schemas
{
    /// <summary>
    /// Represents a "Zone" definition in the IANA tz database files.
    /// </summary>
    internal class IanaTimeZoneEntry
    {
        private static readonly Regex UntilParser = new Regex("(\\d+)(?:[ \\t]+(\\S+))?(?:[ \\t]+(\\S+))?(?:[ \\t]+(\\d{1,2}))?(?::(\\d{2}))?(?::(\\d{2}))?([wgzus])?");

        public IanaTimeZoneEntry(string name, TimeSpan gmtOffset, string rules, string format, string until)
        {
            Name = name;
            GMTOffset = gmtOffset;
            Rules = rules;
            Format = format;
            Until = until;
            CalculateZoneBoundEnding();
        }

        public string Name { get; internal set; }
        public TimeSpan GMTOffset { get; internal set; }
        public string Rules { get; internal set; }
        public string Format { get; internal set; }
        public string Until { get; internal set; }
        public DateTimeOffset ZoneBoundEnding { get; internal set; }

        private void CalculateZoneBoundEnding()
        {
            if (string.IsNullOrEmpty(Until))
            {
                ZoneBoundEnding = DateTimeOffset.MaxValue;
                return;
            }

            Match match = UntilParser.Match(Until);
            if (!match.Success)
            {
                throw new FormatException("Cannot parse UNTIL string " + Until);
            }

            // Group 1: Year
            int year = int.Parse(match.Groups[1].Value);

            // We have to round gmt offset to the nearest minute because datetimeoffset doesn't like seconds offset
            TimeSpan gmtOffsetRoundedToMinute = TimeZoneHelpers.RoundOffsetToNearestMinute(GMTOffset);
            DateTimeOffset returnVal = new DateTimeOffset(year, 1, 1, 0, 0, 0, gmtOffsetRoundedToMinute);

            // Group 2: Month name
            int month = 1;
            if (match.Groups[2].Success)
            {
                month = TimeZoneHelpers.ParseMonthName(match.Groups[2].Value);
                returnVal = returnVal.AddMonths(month - 1);
            }

            // Group 3: Day of month (number or specification)
            int dayOfMonth = 1;
            if (match.Groups[3].Success)
            {
                string daySpec = match.Groups[3].Value;

                if (daySpec.Contains(">="))
                {
                    int dayOfWeek = TimeZoneHelpers.ParseDayOfWeekName(daySpec.Substring(0, 3));
                    int onDayOfMonth = int.Parse(daySpec.Substring(5));

                    returnVal = returnVal.AddDays(onDayOfMonth - 1);
                    int daysNext = dayOfWeek - (int)returnVal.DayOfWeek;
                    if (daysNext < 0)
                    {
                        daysNext += 7;
                    }

                    // zoom to next __day
                    returnVal = returnVal.AddDays(daysNext);
                }
                else if (daySpec.StartsWith("last"))
                {
                    int dayOfWeek = TimeZoneHelpers.ParseDayOfWeekName(daySpec.Substring(4));

                    returnVal = returnVal.AddMonths(1);
                    // zoom to previous __day
                    int daysPrev = (int)returnVal.DayOfWeek - dayOfWeek;
                    if (daysPrev <= 0)
                    {
                        daysPrev += 7;
                    }

                    // zoom to previous __day
                    returnVal = returnVal.AddDays(0 - daysPrev);
                }
                else if (int.TryParse(daySpec, out dayOfMonth))
                {
                    returnVal = returnVal.AddDays(dayOfMonth - 1);
                }
                else
                {
                    throw new FormatException("Cannot parse UNTIL string " + Until);
                }
            }

            // Group 4: Hour
            int hour = 0;
            if (match.Groups[4].Success)
            {
                hour = int.Parse(match.Groups[4].Value);
                returnVal = returnVal.AddHours(hour);
            }

            // Group 5: Minute
            int minute = 0;
            if (match.Groups[5].Success)
            {
                minute = int.Parse(match.Groups[5].Value);
                returnVal = returnVal.AddMinutes(minute);
            }

            // Group 6: Second
            int second = 0;
            if (match.Groups[6].Success)
            {
                second = int.Parse(match.Groups[6].Value);
                returnVal = returnVal.AddSeconds(second);
            }

            // Group 7: Clock specifier (s or u, generally)
            if (match.Groups[7].Success)
            {
                // If zone bound is in terms of UTC, reinterpret the return value
                // The default return value assumes "s" (current zone standard time) so we don't have to specifically handle it here
                if (string.Equals("u", match.Groups[7].Value) ||
                    string.Equals("g", match.Groups[7].Value) ||
                    string.Equals("z", match.Groups[7].Value))
                {
                    returnVal = new DateTimeOffset(returnVal.Year, returnVal.Month, returnVal.Day, returnVal.Hour, returnVal.Minute, returnVal.Second, TimeSpan.Zero).ToOffset(gmtOffsetRoundedToMinute);
                }
            }

            ZoneBoundEnding = returnVal;
        }
    }
}
