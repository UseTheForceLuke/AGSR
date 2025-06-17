using System.Globalization;
using System.Text.RegularExpressions;
using FhirService;
using FhirService.Models;

namespace FhirService
{
    public static class FhirDateTimeParser
    {
        private const string DateTimePattern =
@"^(?<year>[1-9]\d{3}|0\d{3})(-(?<month>0[1-9]|1[0-2])(-(?<day>0[1-9]|[12]\d|3[01])(T(?<hour>[01]\d|2[0-3])(:(?<minute>[0-5]\d)(:(?<second>[0-5]\d|60)(\.(?<fraction>\d{1,9}))?)?)?(?<tz>Z|([+-])(0\d|1[0-4]):[0-5]\d|14:00)?)?)?)?$";

        private static readonly Regex DateTimeRegex = new Regex(DateTimePattern, RegexOptions.ExplicitCapture);

        public static (DateTimeOffset? parsedDate, TimeSpan? originalOffset, DateTimePrecision precision) ParseFhirDateTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException($"{nameof(input)} should not be empty on null");

            var match = DateTimeRegex.Match(input);
            if (!match.Success)
                throw new FormatException($"Invalid FHIR date/time format: {input}");

            try
            {
                // Parse components
                var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
                var month = match.Groups["month"].Success ? int.Parse(match.Groups["month"].Value, CultureInfo.InvariantCulture) : 1;
                var day = match.Groups["day"].Success ? int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture) : 1;
                var hour = match.Groups["hour"].Success ? int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture) : 0;
                var minute = match.Groups["minute"].Success ? int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture) : 0;
                var second = match.Groups["second"].Success ? int.Parse(match.Groups["second"].Value, CultureInfo.InvariantCulture) : 0;

                // Handle fractional seconds
                var fraction = match.Groups["fraction"].Success ?
                    match.Groups["fraction"].Value.PadRight(3, '0')[..3] : "0";
                var milliseconds = int.Parse(fraction, CultureInfo.InvariantCulture);

                // Handle timezone
                TimeSpan offset = TimeSpan.Zero;
                TimeSpan? originalOffset = null;
                if (match.Groups["tz"].Success)
                {
                    var tzValue = match.Groups["tz"].Value;
                    if (tzValue == "Z")
                    {
                        offset = TimeSpan.Zero;
                        originalOffset = TimeSpan.Zero;
                    }
                    else
                    {
                        var sign = tzValue[0] == '+' ? 1 : -1;
                        var hours = int.Parse(tzValue.Substring(1, 2), CultureInfo.InvariantCulture);
                        var minutes = int.Parse(tzValue.Substring(4, 2), CultureInfo.InvariantCulture);
                        originalOffset = new TimeSpan(hours * sign, minutes * sign, 0);
                        offset = originalOffset.Value;
                    }
                }

                // Create local DateTime then convert to UTC via DateTimeOffset
                var localDateTime = new DateTime(year, month, day, hour, minute, second, milliseconds, DateTimeKind.Unspecified);
                var result = new DateTimeOffset(localDateTime, offset).ToOffset(TimeSpan.Zero);

                var precision = DeterminePrecision(match);
                return (result, originalOffset, precision);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to parse FHIR date/time: {input}", ex);
            }
        }

        private static DateTimePrecision DeterminePrecision(Match match)
        {
            if (match.Groups["fraction"].Success)
            {
                return DateTimePrecision.Millisecond;
            }
            if (match.Groups["second"].Success)
                return DateTimePrecision.Second;
            if (match.Groups["minute"].Success)
                return DateTimePrecision.Minute;
            if (match.Groups["hour"].Success)
                return DateTimePrecision.Hour;
            if (match.Groups["day"].Success)
                return DateTimePrecision.Day;
            if (match.Groups["month"].Success)
                return DateTimePrecision.Month;
            return DateTimePrecision.Year;
        }
    }

}
