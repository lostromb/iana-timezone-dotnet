using Iana.Timezone.MathExt;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iana.Timezone
{
    /// <summary>
    /// Static time zone helpers
    /// </summary>
    public static class TimeZoneHelpers
    {
        private static readonly IDictionary<string, string> GOOGLE_TO_IANA_MAPPING = new Dictionary<string, string>()
        {
            { "Antarctica/South_Pole", "Antarctica/Mawson" },
            { "America/Buenos_Aires", "America/Argentina/Buenos_Aires" },
            { "America/Cordoba", "America/Argentina/Cordoba" },
            { "America/Jujuy", "America/Argentina/Jujuy" },
            { "America/Catamarca", "America/Argentina/Catamarca" },
            { "America/Mendoza", "America/Argentina/Mendoza" },
            { "America/Coral_Harbour", "America/Atikokan" },
            { "Pacific/Ponape", "Pacific/Pohnpei" },
            { "Atlantic/Faeroe", "Atlantic/Faroe" },
            { "Asia/Calcutta", "Asia/Kolkata" },
            { "Asia/Rangoon", "Asia/Yangon" },
            { "Asia/Katmandu", "Asia/Kathmandu" },
            { "Asia/Saigon", "Asia/Ho_Chi_Minh" },
            { "Africa/Niamey", "Africa/Lagos" },
            { "Africa/Dar_es_Salaam", "Africa/Nairobi" },
            { "Africa/Luanda", "Africa/Lagos" },
            { "Africa/Addis_Ababa", "Africa/Nairobi" },
            { "Africa/Douala", "Africa/Lagos" },
            { "Africa/Porto-Novo", "Africa/Lagos" },
            { "Africa/Nouakchott", "Africa/Abidjan" },
            { "Africa/Lubumbashi", "Africa/Maputo" },
            { "Europe/Bratislava", "Europe/Budapest" },
            { "Africa/Bangui", "Africa/Lagos" },
            { "Africa/Conakry", "Africa/Abidjan" },
            { "Africa/Gaborone", "Africa/Maputo" },
            { "Europe/Skopje", "Europe/Belgrade" },
            { "Africa/Blantyre", "Africa/Maputo" },
            { "Africa/Lusaka", "Africa/Maputo" },
            { "Africa/Asmera", "Africa/Nairobi" },
            { "Arctic/Longyearbyen", "Europe/Oslo" },
            { "Africa/Harare", "Africa/Maputo" },
            { "Asia/Phnom_Penh", "Asia/Bangkok" },
            { "Africa/Bamako", "Africa/Abidjan" },
            { "Africa/Brazzaville", "Africa/Lagos" },
            { "Africa/Mogadishu", "Africa/Nairobi" },
            { "Africa/Libreville", "Africa/Lagos" },
            { "Africa/Kinshasa", "Africa/Lagos" },
            { "Indian/Antananarivo", "Africa/Nairobi" },
            { "Africa/Ouagadougou", "Africa/Abidjan" },
            { "Africa/Kampala", "Africa/Nairobi" },
            { "Asia/Vientiane", "Asia/Bangkok" },
            { "Asia/Aden", "Asia/Riyadh" },
            { "Africa/Dakar", "Africa/Abidjan" },
            { "Asia/Muscat", "Asia/Dubai" },
            { "Africa/Freetown", "Africa/Abidjan" },
            { "Europe/Ljubljana", "Europe/Belgrade" },
            { "Europe/Sarajevo", "Europe/Belgrade" },
            { "Europe/Zagreb", "Europe/Belgrade" },
            { "Europe/Mariehamn", "Europe/Helsinki" },
            { "Indian/Comoro", "Africa/Nairobi" },
            { "America/St_Thomas", "America/Port_of_Spain" },
            { "Africa/Djibouti", "Africa/Nairobi" },
            { "Africa/Bujumbura", "Africa/Maputo" },
            { "Africa/Maseru", "Africa/Johannesburg" },
            { "Africa/Banjul", "Africa/Abidjan" },
            { "Africa/Kigali", "Africa/Maputo" },
            { "Africa/Malabo", "Africa/Lagos" },
            { "Africa/Lome", "Africa/Abidjan" },
            { "Asia/Bahrain", "Asia/Qatar" },
            { "Asia/Kuwait", "Asia/Riyadh" },
            { "America/Montserrat", "America/Port_of_Spain" },
            { "Africa/Mbabane", "Africa/Johannesburg" },
            { "Europe/Podgorica", "Europe/Belgrade" },
            { "America/Antigua", "America/Port_of_Spain" }
        };

        private static readonly IDictionary<string, string> WINDOWS_TO_IANA_MAPPING = new Dictionary<string, string>()
        {
            { "Dateline Standard Time", "Etc/GMT-12" }, // International Date Line West
            { "Samoa Standard Time", "Pacific/Pago_Pago" }, // Midway, Samoa
            { "Hawaiian Standard Time", "Pacific/Honolulu" },
            { "Alaskan Standard Time", "America/Anchorage" }, // There are multiple zones in Alaska but we assume anchorage
            { "Pacific Standard Time", "America/Los_Angeles" },
            { "Mountain Standard Time", "America/Denver" },
            { "Mexico Standard Time 2", "America/Chihuahua" }, 
            { "U.S. Mountain Standard Time", "America/Phoenix" },
            { "Central Standard Time", "America/Chicago" },
            { "Canada Central Standard Time", "America/Yellowknife" }, // Saskatchewan
            { "Mexico Standard Time", "America/Mexico_City" }, // Guadalajara, Mexico City, Monterrey
            { "Central America Standard Time", "America/Merida" }, // Central America - guessing Merida
            { "Eastern Standard Time", "America/New_York" },
            { "U.S. Eastern Standard Time", "America/Indiana/Indianapolis" }, // Indiana (East)
            { "S.A. Pacific Standard Time", "America/Bogota" },
            { "Atlantic Standard Time", "America/Halifax" }, // Atlantic Time (Canada)
            { "S.A. Western Standard Time", "America/Argentina/San_Juan" }, // Georgetown, La Paz, San Juan
            { "Pacific S.A. Standard Time", "America/Santiago" },
            { "Newfoundland and Labrador Standard Time", "America/St_Johns" },
            { "E. South America Standard Time", "America/Sao_Paulo" }, // Brasilia
            { "S.A. Eastern Standard Time", "America/Guyana" }, // Georgetown
            { "Greenland Standard Time", "America/Godthab" },
            { "Mid-Atlantic Standard Time", "Etc/GMT-2" },
            { "Azores Standard Time", "Atlantic/Azores" },
            { "Cape Verde Standard Time", "Atlantic/Cape_Verde" },
            { "GMT Standard Time", "Etc/GMT" },
            { "Greenwich Standard Time", "Atlantic/Reykjavik" },
            { "Central Europe Standard Time", "Europe/Belgrade" },
            { "Central European Standard Time", "Europe/Warsaw" },
            { "Romance Standard Time", "Europe/Brussels" },
            { "W. Europe Standard Time", "Europe/Amsterdam" },
            { "W. Central Africa Standard Time", "Africa/Algiers" }, // Guessing here
            { "E. Europe Standard Time", "Europe/Minsk" },
            { "Egypt Standard Time", "Africa/Cairo" },
            { "FLE Standard Time", "Europe/Helsinki" },
            { "GTB Standard Time", "Europe/Athens" },
            { "Israel Standard Time", "Asia/Jerusalem" },
            { "South Africa Standard Time", "Africa/Johannesburg" },
            { "Russian Standard Time", "Europe/Moscow" },
            { "Arab Standard Time", "Asia/Riyadh" },
            { "E. Africa Standard Time", "Africa/Nairobi" },
            { "Arabic Standard Time", "Asia/Baghdad" },
            { "Iran Standard Time", "Asia/Tehran" },
            { "Arabian Standard Time", "Asia/Dubai" }, // Abu Dhabi
            { "Caucasus Standard Time", "Asia/Yerevan" }, // How different from Armenian Standard Time?
            { "Transitional Islamic State of Afghanistan Standard Time", "Asia/Kabul" },
            { "Ekaterinburg Standard Time", "Asia/Yekaterinburg" },
            { "West Asia Standard Time", "Asia/Tashkent" },
            { "India Standard Time", "Asia/Kolkata" },
            { "Nepal Standard Time", "Asia/Kathmandu" },
            { "Central Asia Standard Time", "Asia/Dhaka" },
            { "Sri Lanka Standard Time", "Asia/Colombo" },
            { "N. Central Asia Standard Time", "Asia/Novosibirsk" },
            { "Myanmar Standard Time", "Asia/Yangon" },
            { "S.E. Asia Standard Time", "Asia/Bangkok" },
            { "North Asia Standard Time", "Asia/Krasnoyarsk" },
            { "China Standard Time", "Asia/Shanghai" },
            { "Singapore Standard Time", "Asia/Singapore" }, // also Asia/Kuala_Lumpur
            { "Taipei Standard Time", "Asia/Taipei" },
            { "W. Australia Standard Time", "Australia/Perth" },
            { "North Asia East Standard Time", "Asia/Irkutsk" },
            { "Korea Standard Time", "Asia/Seoul" },
            { "Tokyo Standard Time", "Asia/Tokyo" },
            { "Yakutsk Standard Time", "Asia/Yakutsk" },
            { "A.U.S. Central Standard Time", "Australia/Darwin" },
            { "Cen. Australia Standard Time", "Australia/Adelaide" },
            { "A.U.S. Eastern Standard Time", "Australia/Sydney" }, // also Australia/Melbourne
            { "E. Australia Standard Time", "Australia/Brisbane" },
            { "Tasmania Standard Time", "Australia/Hobart" },
            { "Vladivostok Standard Time", "Asia/Vladivostok" },
            { "West Pacific Standard Time", "Pacific/Guam" },
            { "Central Pacific Standard Time", "Asia/Magadan" },
            { "Fiji Islands Standard Time", "Pacific/Fiji" },
            { "New Zealand Standard Time", "Pacific/Auckland" },
            { "Tonga Standard Time", "Pacific/Tongatapu" },
            { "Azerbaijan Standard Time ", "Asia/Baku" },
            { "Middle East Standard Time", "Asia/Beirut" },
            { "Jordan Standard Time", "Asia/Amman" },
            //{ "Central Standard Time", "America/Monterrey" }, // How different from Mexico City? (GMT-06:00) Guadalajara, Mexico City, Monterrey - New
            //{ "Mountain Standard Time", "America/Chihuahua" }, // How different from mexico standard 2? (GMT-07:00) Chihuahua, La Paz, Mazatlan - New
            //{ "Pacific Standard Time", "America/Tijuana" }, // FIXME redundant keys in the dictionary - which ones to use?
            { "Namibia Standard Time", "Africa/Windhoek" },
            { "Georgian Standard Time", "Asia/Tbilisi" },
            { "Central Brazilian Standard Time", "America/Manaus" },
            { "Montevideo Standard Time", "America/Montevideo" },
            { "Armenian Standard Time", "Asia/Yerevan" },
            { "Venezuela Standard Time", "America/Caracas" },
            { "Argentina Standard Time", "America/Argentina/Buenos_Aires" },
            { "Morocco Standard Time", "Africa/Casablanca" },
            { "Pakistan Standard Time", "Asia/Karachi" },
            { "Mauritius Standard Time", "Indian/Mauritius" },
            { "UTC", "Etc/UTC" },
            { "Paraguay Standard Time", "America/Asuncion" },
            { "Kamchatka Standard Time", "Asia/Kamchatka" }
        };

        /// <summary>
        /// Given a Windows timezone name (e.g. "Pakistan Standard Time"), return the closest correlating IANA timezone (e.g. "Asia/Karachi").
        /// If not found, return empty string
        /// </summary>
        /// <param name="windowsTimezoneName"></param>
        /// <returns></returns>
        public static string MapWindowsToIANATimeZone(string windowsTimezoneName)
        {
            if (WINDOWS_TO_IANA_MAPPING.ContainsKey(windowsTimezoneName))
            {
                return WINDOWS_TO_IANA_MAPPING[windowsTimezoneName];
            }

            return string.Empty;
        }

        public static string MapGoogleToIANATimeZone(string googleTimezoneName)
        {
            if (GOOGLE_TO_IANA_MAPPING.ContainsKey(googleTimezoneName))
            {
                return GOOGLE_TO_IANA_MAPPING[googleTimezoneName];
            }

            return string.Empty;
        }

        /// <summary>
        /// Calculates the time zone for the given coordinate based on rounding to the nearest GMT offset. Generally only used to calculate
        /// the local time of an uninhabited place in the middle of the ocean
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="utcTime"></param>
        /// <returns></returns>
        internal static TimeZoneQueryResult CalculateMarinersTime(GeoCoordinate coordinate, DateTimeOffset utcTime)
        {
            int hoursOffset = (int)Math.Round(coordinate.Longitude * 24 / 360);
            TimeSpan utcOffset = TimeSpan.FromHours(hoursOffset);

            string zoneName;
            if (hoursOffset == 0)
            {
                zoneName = "GMT";
            }
            else
            {
                zoneName = string.Format("GMT{0}{1}", hoursOffset < 0 ? "" : "+", hoursOffset);
            }

            return new TimeZoneQueryResult()
            {
                QueryCoordinate = coordinate,
                TimeZoneBaseCoordinate = coordinate,
                DstOffset = utcOffset,
                GmtOffset = utcOffset,
                LocalTime = utcTime.ToOffset(utcOffset),
                TimeZoneName = zoneName,
                TimeZoneAbbreviation = zoneName
            };
        }

        /// <summary>
        /// Parses a month name string in the form "Jan" into a 1-based integer
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static int ParseMonthName(string name)
        {
            if (string.Equals("Jan", name, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            else if (string.Equals("Feb", name, StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
            else if (string.Equals("Mar", name, StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }
            else if (string.Equals("Apr", name, StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }
            else if (string.Equals("May", name, StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }
            else if (string.Equals("Jun", name, StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }
            else if (string.Equals("Jul", name, StringComparison.OrdinalIgnoreCase))
            {
                return 7;
            }
            else if (string.Equals("Aug", name, StringComparison.OrdinalIgnoreCase))
            {
                return 8;
            }
            else if (string.Equals("Sep", name, StringComparison.OrdinalIgnoreCase))
            {
                return 9;
            }
            else if (string.Equals("Oct", name, StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }
            else if (string.Equals("Nov", name, StringComparison.OrdinalIgnoreCase))
            {
                return 11;
            }
            else if (string.Equals("Dec", name, StringComparison.OrdinalIgnoreCase))
            {
                return 12;
            }
            else
            {
                throw new ArgumentException("Could not parse month name \"" + name + "\"");
            }
        }

        /// <summary>
        /// Parses a day of week name string in the form "Mon" into a 0-based integer (C# numbering, not ISO)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static int ParseDayOfWeekName(string name)
        {
            if (string.Equals("Sun", name, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
            else if (string.Equals("Mon", name, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            else if (string.Equals("Tue", name, StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
            else if (string.Equals("Wed", name, StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }
            else if (string.Equals("Thu", name, StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }
            else if (string.Equals("Fri", name, StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }
            else if (string.Equals("Sat", name, StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }
            else
            {
                throw new ArgumentException("Could not parse day of week name \"" + name + "\"");
            }
        }

        /// <summary>
        /// Rounds time span DST offsets to the nearest minute and enforces boundaries so they can be used to create C# DateTimeOffset objects
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static TimeSpan RoundOffsetToNearestMinute(TimeSpan input)
        {
            TimeSpan returnVal = TimeSpan.FromMinutes((double)(int)Math.Round(input.TotalSeconds / 60));
            if (returnVal < TimeSpan.FromHours(-14))
            {
                return TimeSpan.FromHours(-14);
            }
            else if (returnVal > TimeSpan.FromHours(14))
            {
                return TimeSpan.FromHours(14);
            }
            else
            {
                return returnVal;
            }
        }
    }
}
