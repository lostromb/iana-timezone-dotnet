using Iana.Timezone.MathExt;
using Iana.Timezone.Schemas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Iana.Timezone
{
    /// <summary>
    /// Time zone / local time resolver based on IANA published time zone rule files. Can be used to calculate local time and scheduled DST rule transitions for any global timezone for which data is available.
    /// This class relies primarily on freely available IANA rule files which can be downloaded from https://www.iana.org/time-zones
    /// </summary>
    public class TimeZoneResolver
    {
        // Used for parsing input files
        private static readonly Regex RULE_MATCHER = new Regex("^Rule\\s+(.+?)\\s+(.+?)\\t+(.+?)\\t(.+?)\\t(.+?)\\t(.+?)\\t(.+?)\\t(.+?)\\t(.+?)$");
        private static readonly Regex ZONE_MATCHER = new Regex("^Zone\\s+(.+?)\\s+(.+?)\\s+(.+?)\\s(.+?)(?:\\t(.+?))?$");
        private static readonly Regex LINK_MATCHER = new Regex("^Link\\s+(.+?)\\s+(.+?)\\s*$");
        private static readonly Regex ZONE_CONTINUATION_MATCHER = new Regex("^\\t+(.+?)\\s+(.+?)\\s(.+?)(\\t(.+?))?$");
        private static readonly Regex COORDINATE_MATCHER = new Regex("([\\-\\+])(\\d{2})(\\d{2})?(\\d{2})?([\\-\\+])(\\d{3})(\\d{2})?(\\d{2})?");

        /// <summary>
        /// Maximum distance we may be from a known time zone geolocation in order to say that it governs "local time".
        /// For example, time zones aren't really defined in the middle of the ocean.
        /// </summary>
        private const double MAX_GEO_DISTANCE_KM = 100;

        /// <summary>
        /// Index of all Zone entities found in IANA database, keyed by zone name
        /// </summary>
        private Dictionary<string, List<IanaTimeZoneEntry>> _zones;

        /// <summary>
        /// Index of all Rule entities found in IANA database, keyed by rule name
        /// </summary>
        private Dictionary<string, List<IanaTimeZoneRule>> _rules;

        /// <summary>
        /// Map of geographic points that correlate to individual IANA time zones
        /// </summary>
        private DynamicQuadtree<TimeZonePoint> _geoPoints;

        /// <summary>
        /// Mapping of IANA zone name to zone metadata (primary coordinate, countries, etc.)
        /// </summary>
        private Dictionary<string, IanaTimeZoneMetadata> _zoneMeta;

        /// <summary>
        /// Maps zone links from source => destination zone name
        /// </summary>
        private IDictionary<string, string> _zoneLinks;

        /// <summary>
        /// Constructs a new time zone resolver
        /// </summary>
        /// <param name="logger"></param>
        public TimeZoneResolver()
        {
            _zoneLinks = new Dictionary<string, string>();
            _zoneMeta = new Dictionary<string, IanaTimeZoneMetadata>();
            _zones = new Dictionary<string, List<IanaTimeZoneEntry>>();
            _rules = new Dictionary<string, List<IanaTimeZoneRule>>();
        }
        
        /// <summary>
        /// Initializes the time zone resolver and TZ point database.
        /// This is an asynchronous method because of file access but it also performs a lot of parsing and geo calculation to prepare the data
        /// </summary>
        /// <param name="fileSystem">A local filesystem to load data files from</param>
        /// <param name="ianaFileDirectory">The path to a folder containing AT MINIMUM: zone1970.tab and iana time zone definition files (usually named as continents without file extension).
        /// Additionally, loads TimeZoneGlobalPoints.tsv if available to provide time zone geographic mapping</param>
        /// <returns></returns>
        public bool Initialize(DirectoryInfo ianaFileDirectory)
        {
            if (!ianaFileDirectory.Exists)
            {
                Debug.WriteLine("Required directory " + ianaFileDirectory.FullName + " not found!");
                return false;
            }

            string zoneDefinitionFile = Path.Combine(ianaFileDirectory.FullName, "zone1970.tab");
            if (!File.Exists(zoneDefinitionFile))
            {
                Debug.WriteLine("Required file " + zoneDefinitionFile + " not found!");
                return false;
            }

            ParseZone1970File(new FileInfo(zoneDefinitionFile));

            foreach (FileInfo ianaFile in ianaFileDirectory.EnumerateFiles())
            {
                if (string.IsNullOrEmpty(ianaFile.Extension))
                {
                    // Assume it is a rules + zones file
                    ParseRulesFile(ianaFile);
                }
                else if (!string.Equals("zone1970.tab", ianaFile.Name, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals("iso3166.tab", ianaFile.Name, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals("TimeZoneGlobalPoints.tsv", ianaFile.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("Unknown file in IANA directory; ignoring: " + ianaFile.FullName);
                }
            }
            
            List<TimeZonePoint> rawGeoPoints = new List<TimeZonePoint>();
            string globalPointsFile = Path.Combine(ianaFileDirectory.FullName, "TimeZoneGlobalPoints.tsv");
            if (File.Exists(globalPointsFile))
            {
                rawGeoPoints = ParseGeoPointsFile(new FileInfo(globalPointsFile));
            }
            else
            {
                Debug.WriteLine("Global points database \"" + globalPointsFile + "\" not found! Geographic timezone resolution will be greatly degraded");
                // If global points file is not available, use the representative city data from the zone1970.tab file as the global points. It's degenerate but better than nothing
                foreach (var ianaZone in _zoneMeta.Values)
                {
                    rawGeoPoints.Add(new TimeZonePoint(ianaZone.PrincipalCoordinate, ianaZone.ZoneName));
                }
            }

            #region old
            // Old code that would try and rectify non-IANA zone names in the input global points file.
            // Since the data was updated to be strict IANA this should no longer be needed
            // Correlate IANA metadata with the geo grid that we loaded earlier. Use that to determine the precise extent of a zone
            //var clusteredGeoGrid = new Dictionary<string, List<TimeZonePoint>>();
            //foreach (var point in rawGeoPoints)
            //{
            //    if (!clusteredGeoGrid.ContainsKey(point.TimeZoneId))
            //    {
            //        clusteredGeoGrid[point.TimeZoneId] = new List<TimeZonePoint>();
            //    }

            //    clusteredGeoGrid[point.TimeZoneId].Add(point);
            //}

            //Dictionary<string, HashSet<string>> geoClusterToIanaZoneMapping = new Dictionary<string, HashSet<string>>();

            //foreach (var ianaZone in _zoneMeta.Values)
            //{
            //    TimeZonePoint closestPoint = default(TimeZonePoint);
            //    double closestDistance = double.MaxValue;

            //    foreach (var point in rawGeoPoints)
            //    {
            //        double dist = GeoMath.CalculateGeoDistance(point.Coords, ianaZone.PrincipalCoordinate);
            //        if (dist < closestDistance)
            //        {
            //            closestDistance = dist;
            //            closestPoint = point;
            //        }
            //    }

            //    if (!geoClusterToIanaZoneMapping.ContainsKey(closestPoint.TimeZoneId))
            //    {
            //        geoClusterToIanaZoneMapping[closestPoint.TimeZoneId] = new HashSet<string>();
            //    }

            //    if (!geoClusterToIanaZoneMapping[closestPoint.TimeZoneId].Contains(ianaZone.ZoneName))
            //    {
            //        geoClusterToIanaZoneMapping[closestPoint.TimeZoneId].Add(ianaZone.ZoneName);
            //    }
            //}

            // Reassign time zone points to IANA zones rather than the old zones
            //List<TimeZonePoint> ianaGeoPoints = new List<TimeZonePoint>();
            //foreach (string geoClusterId in geoClusterToIanaZoneMapping.Keys)
            //{
            //    HashSet<string> ianaZones = geoClusterToIanaZoneMapping[geoClusterId];
            //    if (ianaZones.Count == 1)
            //    {
            //        string singleZone = ianaZones.First();
            //        _logger.Log("GEOMAP " + geoClusterId + " @@@ " + singleZone);
            //        foreach (TimeZonePoint point in rawGeoPoints)
            //        {
            //            if (string.Equals(geoClusterId, point.TimeZoneId))
            //            {
            //                ianaGeoPoints.Add(new TimeZonePoint()
            //                {
            //                    Coords = point.Coords,
            //                    TimeZoneId = singleZone
            //                });
            //            }
            //        }
            //    }
            //    else
            //    {
            //        _logger.Log("GEOMAP " + geoClusterId + " @@@ " + string.Join(",", ianaZones), LogLevel.Wrn);

            //        //Counter<string> density = new Counter<string>();
            //        //foreach (TimeZonePoint point in rawGeoPoints)
            //        //{
            //        //    if (string.Equals(geoClusterId, point.TimeZoneId))
            //        //    {
            //        //        density.Increment(point.TimeZoneId);
            //        //    }
            //        //}

            //        foreach (TimeZonePoint point in rawGeoPoints)
            //        {
            //            if (string.Equals(geoClusterId, point.TimeZoneId))
            //            {
            //                string closestZone = null;
            //                double closestDistance = double.MaxValue;
            //                foreach (IanaTimeZoneMetadata zoneMeta in _zoneMeta.Values)
            //                {
            //                    if (ianaZones.Contains(zoneMeta.ZoneName))
            //                    {
            //                        double dist = GeoMath.CalculateGeoDistance(point.Coords, zoneMeta.PrincipalCoordinate);
            //                        //dist /= density.GetCount(point.TimeZoneId); // TODO Augment distance by the relative density of each respective zone, so a single point doesn't take the share of half of a continent
            //                        if (dist < closestDistance)
            //                        {
            //                            closestDistance = dist;
            //                            closestZone = zoneMeta.ZoneName;
            //                        }
            //                    }
            //                }

            //                ianaGeoPoints.Add(new TimeZonePoint()
            //                {
            //                    Coords = point.Coords,
            //                    TimeZoneId = closestZone
            //                });
            //            }
            //        }
            //    }

            //    // TODO catch geoclusters that have no IANA zone (there's lots of them)
            //}
            #endregion

            // Put all the geopoints onto a quadtree for easy indexing
            _geoPoints = new DynamicQuadtree<TimeZonePoint>();
            foreach (TimeZonePoint point in rawGeoPoints) // ianaGeoPoints
            {
                _geoPoints.AddItem(point, new Vector2f((float)point.Coords.Longitude, (float)point.Coords.Latitude));
                
                // Simulate wraparound at the antimeridian by duplicating points
                if (point.Coords.Longitude < -150)
                {
                    _geoPoints.AddItem(point, new Vector2f((float)point.Coords.Longitude + 360, (float)point.Coords.Latitude));
                }
                else if (point.Coords.Longitude > 150)
                {
                    _geoPoints.AddItem(point, new Vector2f((float)point.Coords.Longitude - 360, (float)point.Coords.Latitude));
                }
            }
            
            return true;
        }

        /// <summary>
        /// Given an IANA time zone name and span of interest, calculate a set of contiguous time zone spans which describe how various DST / GMT offset rules
        /// have applied within that range. For example, if you query from Jan 1 to Dec 31 of a particular year in a zone that has DST, you will get 3 spans in the response:
        /// a non-DST span from Jan 1 to spring, a DST span from spring to autumn, and a non-DST span from autumn to Dec 31.
        /// This can be used to calculate the current local time (though this class already provides an easier helper method to do that) as well as to answer questions such as "when is the next
        /// impending clock change?"
        /// </summary>
        /// <param name="timeZoneName">The IANA name of the timezone to query, for example Europe/Madrid</param>
        /// <param name="rangeBegin">The beginning of the query range</param>
        /// <param name="rangeEnd">The ending of the query range</param>
        /// <param name="queryLogger">A logger</param>
        /// <returns>An ordered set of time zone rule spans which describe DST / GMT rules and the boundaries of when those rules take effect</returns>
        public List<TimeZoneRuleEffectiveSpan> CalculateTimeZoneRuleSpans(string timeZoneName, DateTimeOffset rangeBegin, DateTimeOffset rangeEnd)
        {
            string actualZoneName = ResolveTimeZoneName(timeZoneName);
            if (string.IsNullOrEmpty(actualZoneName))
            {
                Debug.WriteLine("No results for timezone \"" + timeZoneName + "\", most likely the zone name is unknown");
                return null;
            }

            List<TimeZoneRuleSetRegion> zoneRegions = CalculateTimeZoneRegions(actualZoneName, rangeBegin, rangeEnd);
            if (zoneRegions == null || zoneRegions.Count == 0)
            {
                return null;
            }

            List<TimeZoneRuleEffectiveSpan> returnVal = new List<TimeZoneRuleEffectiveSpan>();
            foreach (TimeZoneRuleSetRegion region in zoneRegions)
            {
                List<TimeZoneRuleEffectiveSpan> ruleBoundsWithinThisRegion = GenerateTimeZoneRuleRegionsForSingleZone(region.ZoneDef, region.RangeBegin, region.RangeEnd);
                returnVal.AddRange(ruleBoundsWithinThisRegion);
            }

            return returnVal;
        }

        /// <summary>
        /// Given a geo coordinate and the current UTC time, calculate the local time at that location
        /// </summary>
        /// <param name="coordinate">The coordinate to query</param>
        /// <param name="utcTime">The current UTC time</param>
        /// <param name="queryLogger">A logger</param>
        /// <returns>Time zone query results, or null if an internal error occurred</returns>
        public TimeZoneQueryResult CalculateLocalTime(GeoCoordinate coordinate, DateTimeOffset utcTime)
        {
            // Resolve geolocation into a list of zones
            // Debug.WriteLine("Resolving geolocation " + coordinate.Latitude + "," + coordinate.Longitude + " into a timezone...");
            string zoneName = FindTimeZoneByGeolocation(coordinate);

            if (zoneName == null)
            {
                Debug.WriteLine("No time zone found! Falling back to mariners' time");
                return TimeZoneHelpers.CalculateMarinersTime(coordinate, utcTime);
            }

            TimeZoneQueryResult returnVal = CalculateLocalTime(zoneName, utcTime);
            if (returnVal != null)
            {
                returnVal.QueryCoordinate = coordinate;
            }

            return returnVal;
        }

        /// <summary>
        /// Given an IANA time zone name and the current UTC time, calculate the local time in that timezone
        /// </summary>
        /// <param name="zoneName">The IANA name of the time zone, for example Africa/Cairo</param>
        /// <param name="utcTime">The current UTC time</param>
        /// <param name="queryLogger">A logger</param>
        /// <returns>Time zone query results, or null if an internal error occurred</returns>
        public TimeZoneQueryResult CalculateLocalTime(string zoneName, DateTimeOffset utcTime)
        {
            string actualZoneName = ResolveTimeZoneName(zoneName);
            if (string.IsNullOrEmpty(actualZoneName))
            {
                Debug.WriteLine("No results for timezone \"" + zoneName + "\", most likely the zone name is unknown");
                return null;
            }

            IanaTimeZoneMetadata zoneMeta;
            if (!_zoneMeta.TryGetValue(zoneName, out zoneMeta) &&
                !_zoneMeta.TryGetValue(actualZoneName, out zoneMeta))
            {
                Debug.WriteLine("No metadata found for zone \"" + zoneName + "\"");
                return null;
            }

            List<TimeZoneRuleEffectiveSpan> spans = CalculateTimeZoneRuleSpans(zoneName, utcTime - TimeSpan.FromDays(5), utcTime + TimeSpan.FromDays(5));
            if (spans == null || spans.Count == 0)
            {
                Debug.WriteLine("No results for timezone \"" + zoneName + "\", no rules are defined?");
                return null;
            }

            // Find the span the covers the requested time
            foreach (TimeZoneRuleEffectiveSpan span in spans)
            {
                if (utcTime < span.RuleBoundaryEnd &&
                    utcTime >= span.RuleBoundaryBegin)
                {
                    return new TimeZoneQueryResult()
                    {
                        DstOffset = span.DstOffset,
                        GmtOffset = span.GmtOffset,
                        LocalTime = utcTime.ToOffset(TimeZoneHelpers.RoundOffsetToNearestMinute(span.DstOffset + span.GmtOffset)),
                        TimeZoneAbbreviation = span.TimeZoneAbbreviation,
                        TimeZoneName = zoneName,
                        QueryCoordinate = zoneMeta.PrincipalCoordinate,
                        TimeZoneBaseCoordinate = zoneMeta.PrincipalCoordinate
                    };
                }
            }

            // Shouldn't happen but just in case
            Debug.WriteLine("No time zone rule span was found which contains the query time " + utcTime.ToString() + " for zone " + zoneName);
            return null;
        }

        /// <summary>
        /// Given a range of dates and a time zone name, return a list of time spans for which different timezone rulesets applied to this zone
        /// </summary>
        /// <param name="zoneName"></param>
        /// <param name="rangeBegin"></param>
        /// <param name="rangeEnd"></param>
        /// <param name="queryLogger"></param>
        /// <returns></returns>
        private List<TimeZoneRuleSetRegion> CalculateTimeZoneRegions(string zoneName, DateTimeOffset rangeBegin, DateTimeOffset rangeEnd)
        {
            List<IanaTimeZoneEntry> zoneList = _zones[zoneName];

            if (zoneList == null || zoneList.Count == 0)
            {
                Debug.WriteLine("No time zone named \"" + zoneName + "\" found! Cannot return a valid result");
                return null;
            }
            
            // Step 1 - find out what span of rulesets apply to the given range
            List<TimeZoneRuleSetRegion> spannedRuleSets = new List<TimeZoneRuleSetRegion>();
            DateTimeOffset ruleSetBegin = DateTimeOffset.MinValue;
            DateTimeOffset ruleSetEnd;
            foreach (IanaTimeZoneEntry zone in zoneList)
            {
                ruleSetEnd = zone.ZoneBoundEnding;

                if (rangeBegin < ruleSetEnd &&
                        rangeEnd > ruleSetBegin)
                {
                    spannedRuleSets.Add(new TimeZoneRuleSetRegion()
                    {
                        RangeBegin = ruleSetBegin < rangeBegin ? rangeBegin : ruleSetBegin,
                        RangeEnd = ruleSetEnd > rangeEnd ? rangeEnd : ruleSetEnd,
                        ZoneDef = zone
                    });
                }

                ruleSetBegin = ruleSetEnd;
            }

            return spannedRuleSets;
        }

        /// <summary>
        /// Produces a sorted list of time spans for which various DST rules take effect according to the set of rules specified in the given zone.
        /// The return value is guaranteed to be a set of spans which cover the entire queried region
        /// </summary>
        /// <param name="zone">Information about the current timezone</param>
        /// <param name="rangeBegin">The beginning of the range you are interested in</param>
        /// <param name="rangeEnd">The end of the range you are interested in</param>
        /// <param name="queryLogger">A logger</param>
        /// <returns>A list of daylight savings time rules and the spans for which they take efffect</returns>
        private List<TimeZoneRuleEffectiveSpan> GenerateTimeZoneRuleRegionsForSingleZone(IanaTimeZoneEntry zone, DateTimeOffset rangeBegin, DateTimeOffset rangeEnd)
        {
            if (_rules.ContainsKey(zone.Rules))
            {
                List<IanaTimeZoneRule> rulesToApply = _rules[zone.Rules];

                // Find the list of all rule times around the query time and put them into sorted order
                List<DstRuleAndEffectiveTime> ruleEffectiveTimes = new List<DstRuleAndEffectiveTime>();
                for (int year = Math.Max(1, rangeBegin.Year - 1); year <= Math.Min(9999, rangeEnd.Year + 1); year++)
                {
                    foreach (IanaTimeZoneRule rule in rulesToApply)
                    {
                        DateTimeOffset? effectiveTime = rule.GetRuleEffectiveTime(year);
                        if (effectiveTime.HasValue)
                        {
                            ruleEffectiveTimes.Add(new DstRuleAndEffectiveTime(effectiveTime.Value, rule));
                        }
                    }
                }

                if (ruleEffectiveTimes.Count == 0)
                {
                    // No DST rules apply in this time window at all. So just return a synthetic value
                    TimeSpan safeOffset = TimeZoneHelpers.RoundOffsetToNearestMinute(zone.GMTOffset);
                    return new List<TimeZoneRuleEffectiveSpan>()
                    {
                        new TimeZoneRuleEffectiveSpan()
                        {
                            RuleBoundaryBegin = rangeBegin.ToOffset(safeOffset),
                            RuleBoundaryEnd = rangeEnd.ToOffset(safeOffset),
                            DstOffset = TimeSpan.Zero,
                            GmtOffset = zone.GMTOffset,
                            TimeZoneAbbreviation = zone.Format.Replace("%s", string.Empty)
                        }
                    };
                }

                ruleEffectiveTimes.Sort();

                // Now walk through the rules in order to find boundaries (we have to do this because the effective time of most rules is relative to the local time defined in previous rules)
                DateTimeOffset currentBoundaryTimeStart = new DateTimeOffset(Math.Max(1, rangeBegin.Year - 1), 1, 1, 0, 0, 0, zone.GMTOffset);
                DateTimeOffset currentBoundaryTimeEnd = new DateTimeOffset(Math.Min(9999, rangeEnd.Year + 1), 1, 1, 0, 0, 0, zone.GMTOffset);
                IanaTimeZoneRule effectiveRule = null;
                List<TimeZoneRuleEffectiveSpan> returnVal = new List<TimeZoneRuleEffectiveSpan>();

                foreach (var ruleEffectiveTime in ruleEffectiveTimes)
                {
                    TimeSpan effectiveOffset;
                    if (ruleEffectiveTime.Rule.AtModifier == ClockType.Universal)
                    {
                        effectiveOffset = TimeSpan.Zero;
                    }
                    else if (ruleEffectiveTime.Rule.AtModifier == ClockType.LocalStandardTime)
                    {
                        effectiveOffset = zone.GMTOffset;
                    }
                    else
                    {
                        effectiveOffset = currentBoundaryTimeStart.Offset;
                    }

                    currentBoundaryTimeEnd = new DateTimeOffset(
                        ruleEffectiveTime.RuleEffectiveTime.Year,
                        ruleEffectiveTime.RuleEffectiveTime.Month,
                        ruleEffectiveTime.RuleEffectiveTime.Day,
                        ruleEffectiveTime.RuleEffectiveTime.Hour,
                        ruleEffectiveTime.RuleEffectiveTime.Minute,
                        ruleEffectiveTime.RuleEffectiveTime.Second,
                        effectiveOffset);
                    
                    // queryLogger.Log("Effective period for " + ruleEffectiveTime.Rule.ToString() + "   is   " + currentBoundaryTimeStart + "   ->   " + currentBoundaryTimeEnd, LogLevel.Vrb);
                    if (rangeBegin < currentBoundaryTimeEnd &&
                        rangeEnd > currentBoundaryTimeStart)
                    {
                        TimeZoneRuleEffectiveSpan thisRegion = new TimeZoneRuleEffectiveSpan();
                        thisRegion.RuleBoundaryBegin = currentBoundaryTimeStart < rangeBegin ? rangeBegin : currentBoundaryTimeStart;
                        thisRegion.RuleBoundaryEnd = currentBoundaryTimeEnd > rangeEnd ? rangeEnd : currentBoundaryTimeEnd;
                        thisRegion.GmtOffset = zone.GMTOffset;

                        if (effectiveRule != null)
                        {
                            thisRegion.DstOffset = effectiveRule.Save;
                            thisRegion.TimeZoneAbbreviation = zone.Format.Replace("%s", effectiveRule.Letter);
                        }
                        else
                        {
                            thisRegion.DstOffset = TimeSpan.Zero;
                            thisRegion.TimeZoneAbbreviation = zone.Format.Replace("%s", string.Empty); // FIXME What is the expected behavior here? For example, Los Angeles time between 1883 and 1918, the format is P%sT but no rules apply
                        }

                        returnVal.Add(thisRegion);
                    }

                    if (rangeEnd < currentBoundaryTimeStart)
                    {
                        break;
                    }

                    effectiveRule = ruleEffectiveTime.Rule;
                    currentBoundaryTimeStart = currentBoundaryTimeEnd.ToOffset(TimeZoneHelpers.RoundOffsetToNearestMinute(zone.GMTOffset + effectiveRule.Save));
                }

                // Fill in the end cap by extending the current range to the end of the requested span
                if (currentBoundaryTimeEnd < rangeEnd)
                {
                    TimeZoneRuleEffectiveSpan endSpan = new TimeZoneRuleEffectiveSpan();
                    endSpan.GmtOffset = zone.GMTOffset;

                    if (effectiveRule != null)
                    {
                        endSpan.DstOffset = effectiveRule.Save;
                        endSpan.TimeZoneAbbreviation = zone.Format.Replace("%s", effectiveRule.Letter);
                    }
                    else
                    {
                        endSpan.DstOffset = TimeSpan.Zero;
                        endSpan.TimeZoneAbbreviation = zone.Format.Replace("%s", string.Empty);
                    }

                    TimeSpan roundedOffset = TimeZoneHelpers.RoundOffsetToNearestMinute(zone.GMTOffset + endSpan.DstOffset);
                    endSpan.RuleBoundaryBegin = currentBoundaryTimeEnd > rangeBegin ?
                        currentBoundaryTimeEnd.ToOffset(roundedOffset) :
                        rangeBegin.ToOffset(roundedOffset);
                    endSpan.RuleBoundaryEnd = rangeEnd.ToOffset(roundedOffset);

                    returnVal.Add(endSpan);
                }

                return returnVal;
            }
            else
            {
                // No rules to apply - just create a region for the entire query bounds
                TimeSpan safeOffset = TimeZoneHelpers.RoundOffsetToNearestMinute(zone.GMTOffset);
                return new List<TimeZoneRuleEffectiveSpan>()
                {
                    new TimeZoneRuleEffectiveSpan()
                    {
                        RuleBoundaryBegin = rangeBegin.ToOffset(safeOffset),
                        RuleBoundaryEnd = rangeEnd.ToOffset(safeOffset),
                        DstOffset = TimeSpan.Zero,
                        GmtOffset = zone.GMTOffset,
                        TimeZoneAbbreviation = zone.Format.Replace("%s", string.Empty)
                    }
                };
            }
        }

        private string FindTimeZoneByGeolocation(GeoCoordinate coordinate)
        {
            string returnVal = null;
            double closestDistance = double.MaxValue;
            foreach (var geoPoint in _geoPoints.GetItemsNearPoint(new Vector2f((float)coordinate.Longitude, (float)coordinate.Latitude)))
            {
                double dist = GeoMath.CalculateGeoDistance(coordinate, geoPoint.Item1.Coords);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    returnVal = geoPoint.Item1.TimeZoneId;
                }
            }

            // If distance from the nearest point is way huge, assume they are in the middle of the ocean and just use Mariners' time
            if (closestDistance > MAX_GEO_DISTANCE_KM)
            {
                return null;
            }

            return ResolveTimeZoneName(returnVal);
        }

        private string ResolveTimeZoneName(string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName))
            {
                return null;
            }

            if (_zoneLinks.ContainsKey(zoneName))
            {
                zoneName = _zoneLinks[zoneName];
            }

            if (_zones.ContainsKey(zoneName))
            {
                return zoneName;
            }

            return null;
        }

        #region File parsers
        
        private void ParseRulesFile(FileInfo inputFile)
        {
            string currentZoneName = null;
            using (Stream fileStream = new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    int lineNum = 0;
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        lineNum++;

                        if (line.Contains("#"))
                        {
                            line = line.Substring(0, line.IndexOf('#'));
                        }

                        line = line.TrimEnd();

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            currentZoneName = null;
                            continue;
                        }

                        if (line.StartsWith("Rule"))
                        {
                            currentZoneName = null;
                            Match ruleMatch = RULE_MATCHER.Match(line);
                            // # Rule	NAME	FROM	TO	TYPE	IN	ON	AT	SAVE	LETTER/S
                            //Rule	Algeria	1921	only	-	Mar	14	23:00s	1:00	S

                            if (!ruleMatch.Success)
                            {
                                Debug.WriteLine("Strange line found in " + inputFile.FullName + ":" + lineNum + " " + line);
                                continue;
                            }

                            string ruleName = ruleMatch.Groups[1].Value;
                            IanaTimeZoneRule rule = new IanaTimeZoneRule(
                                ruleMatch.Groups[1].Value,
                                ruleMatch.Groups[2].Value,
                                ruleMatch.Groups[3].Value,
                                ruleMatch.Groups[4].Value,
                                ruleMatch.Groups[5].Value,
                                ruleMatch.Groups[6].Value,
                                ruleMatch.Groups[7].Value,
                                ruleMatch.Groups[8].Value,
                                ruleMatch.Groups[9].Value);

                            if (!_rules.ContainsKey(ruleName))
                            {
                                _rules[ruleName] = new List<IanaTimeZoneRule>();
                            }

                            _rules[ruleName].Add(rule);
                        }
                        else if (line.StartsWith("Link"))
                        {
                            currentZoneName = null;
                            Match linkMatch = LINK_MATCHER.Match(line);

                            if (!linkMatch.Success)
                            {
                                Debug.WriteLine("Strange line found in " + inputFile.FullName + ":" + lineNum + " " + line);
                                continue;
                            }

                            string target = linkMatch.Groups[1].Value;
                            string source = linkMatch.Groups[2].Value;

                            _zoneLinks[source] = target;
                        }
                        else if (line.StartsWith("Zone"))
                        {
                            Match zoneMatch = ZONE_MATCHER.Match(line);
                            if (!zoneMatch.Success)
                            {
                                Debug.WriteLine("Strange line found in " + inputFile.FullName + ":" + lineNum + " " + line);
                                continue;
                            }

                            // Zone NAME                         GMT      RULES FORMAT UNTIL
                            // Zone America/Indiana/Indianapolis -5:44:38 -    LMT	   1883 Nov 18 12:15:22
                            currentZoneName = zoneMatch.Groups[1].Value;
                            IanaTimeZoneEntry zone = new IanaTimeZoneEntry(
                                zoneMatch.Groups[1].Value,
                                TimeSpan.Parse(zoneMatch.Groups[2].Value),
                                zoneMatch.Groups[3].Value,
                                zoneMatch.Groups[4].Value,
                                zoneMatch.Groups[5].Success ? zoneMatch.Groups[5].Value : string.Empty);

                            if (!_zones.ContainsKey(zone.Name))
                            {
                                _zones[zone.Name] = new List<IanaTimeZoneEntry>();
                            }

                            _zones[zone.Name].Add(zone);
                        }
                        else if (line.StartsWith("\t") && currentZoneName != null)
                        {
                            Match zoneMatch = ZONE_CONTINUATION_MATCHER.Match(line);
                            if (!zoneMatch.Success)
                            {
                                Debug.WriteLine("Strange line found in " + inputFile.FullName + ":" + lineNum + " " + line);
                                continue;
                            }

                            IanaTimeZoneEntry zone = new IanaTimeZoneEntry(
                                currentZoneName,
                                TimeSpan.Parse(zoneMatch.Groups[1].Value),
                                zoneMatch.Groups[2].Value,
                                zoneMatch.Groups[3].Value,
                                zoneMatch.Groups[4].Success ? zoneMatch.Groups[5].Value : string.Empty);

                            if (!_zones.ContainsKey(zone.Name))
                            {
                                _zones[zone.Name] = new List<IanaTimeZoneEntry>();
                            }

                            _zones[zone.Name].Add(zone);
                        }
                    }
                }
            }
        }

        private void ParseZone1970File(FileInfo ianaFile)
        {
            using (Stream fileStream = new FileStream(ianaFile.FullName, FileMode. Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        {
                            continue;
                        }

                        string[] tabs = line.Split('\t');
                        if (tabs.Length < 3)
                        {
                            Debug.WriteLine("Bad line " + line + " in file " + ianaFile.FullName);
                        }

                        IanaTimeZoneMetadata metadata = new IanaTimeZoneMetadata();
                        string[] countries = tabs[0].Split(',');
                        metadata.Countries = new HashSet<string>(countries);

                        // Parse lat/long in HHMMSS format and convert to decimal
                        double lat = 0;
                        double lon = 0;
                        Match coordMatch = COORDINATE_MATCHER.Match(tabs[1]);

                        // 1 lat sign
                        // 2 - 4 HHMMSS
                        // 5 lon sign
                        // 6 - 8 HHMMSS
                        lat = double.Parse(coordMatch.Groups[2].Value);
                        if (coordMatch.Groups[3].Success)
                        {
                            lat += double.Parse(coordMatch.Groups[3].Value) / 60;
                        }
                        if (coordMatch.Groups[4].Success)
                        {
                            lat += double.Parse(coordMatch.Groups[4].Value) / 3600;
                        }
                        if (string.Equals("-", coordMatch.Groups[1].Value))
                        {
                            lat = 0 - lat;
                        }

                        lon = double.Parse(coordMatch.Groups[6].Value);
                        if (coordMatch.Groups[7].Success)
                        {
                            lon += double.Parse(coordMatch.Groups[7].Value) / 60;
                        }
                        if (coordMatch.Groups[8].Success)
                        {
                            lon += double.Parse(coordMatch.Groups[8].Value) / 3600;
                        }
                        if (string.Equals("-", coordMatch.Groups[5].Value))
                        {
                            lon = 0 - lon;
                        }
                        metadata.PrincipalCoordinate = new GeoCoordinate(lat, lon);

                        metadata.ZoneName = tabs[2];
                        if (tabs.Length >= 4)
                        {
                            metadata.Comment = tabs[3];
                        }
                        else
                        {
                            metadata.Comment = string.Empty;
                        }

                        _zoneMeta[metadata.ZoneName] = metadata;
                    }
                }
            }
        }

        private List<TimeZonePoint> ParseGeoPointsFile(FileInfo globalPointsFile)
        {
            List<TimeZonePoint> returnVal = new List<TimeZonePoint>();
            if (!globalPointsFile.Exists)
            {
                Debug.WriteLine("Required file " + globalPointsFile.FullName + " not found!");
                return returnVal;
            }

            using (Stream fileStream =  new FileStream(globalPointsFile.FullName, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader pointReader = new StreamReader(fileStream))
                {
                    while (!pointReader.EndOfStream)
                    {
                        string line = pointReader.ReadLine();
                        if (!line.Contains("\t"))
                        {
                            continue;
                        }

                        string[] parts = line.Split('\t');
                        if (parts.Length != 3)
                        {
                            continue;
                        }

                        TimeZonePoint point = new TimeZonePoint(new GeoCoordinate(double.Parse(parts[0]), double.Parse(parts[1])), parts[2]);
                        returnVal.Add(point);
                    }
                }
            }

            return returnVal;
        }

        #endregion
    }
}