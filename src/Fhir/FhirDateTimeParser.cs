//using SharedKernel;
//using System.Globalization;
//using System.Text.RegularExpressions;

//namespace FhirService
//{
//    public static class FhirDateTimeParser
//    {
//        // Regex patterns from HL7 FHIR specification
//        private const string DatePattern =     @"^([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)(-(0[1-9]|1[0-2])(-(0[1-9]|[1-2][0-9]|3[0-1]))?)?$";
//        private const string DateTimePattern = @"^([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)(-(0[1-9]|1[0-2])(-(0[1-9]|[1-2][0-9]|3[0-1])(T([01][0-9]|2[0-3]):[0-5][0-9]:([0-5][0-9]|60)(\.[0-9]{1,9})?)?)?)?(Z|(\+|-)((0[0-9]|1[0-3]):[0-5][0-9]|14:00)?)?$";
//        private const string InstantPattern =  @"^([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)-(0[1-9]|1[0-2])-(0[1-9]|[1-2][0-9]|3[0-1])T([01][0-9]|2[0-3]):[0-5][0-9]:([0-5][0-9]|60)(\.[0-9]{1,9})?(Z|(\+|-)((0[0-9]|1[0-3]):[0-5][0-9]|14:00))$";

//        public static (DateTimeOffset? parsedDate, DateTimePrecision precision) ParseFhirDateTime(string input)
//        {
//            if (string.IsNullOrWhiteSpace(input))
//                return (null, DateTimePrecision.None);

//            // Determine the format and parse accordingly
//            if (Regex.IsMatch(input, InstantPattern))
//            {
//                return ParseWithPrecision(input, DateTimePrecision.Millisecond);
//            }
//            else if (Regex.IsMatch(input, DatePattern))
//            {
//                return ParseDate(input);
//            }
//            else if (Regex.IsMatch(input, DateTimePattern))
//            {
//                return ParseDateTime(input);
//            }
            

//            throw new FormatException($"Invalid FHIR date/time format: {input}");
//        }

//        private static (DateTimeOffset, DateTimePrecision) ParseDateTime(string input)
//        {
//            var hasTimeComponent = input.Contains('T');
//            var hasSeconds = hasTimeComponent && input.Substring(input.IndexOf('T')).Contains(':');
//            var hasMilliseconds = hasTimeComponent && input.Contains('.');
//            var hasTimezone = input.EndsWith('Z') || input.Contains('+') || input.Contains('-');

//            var format = DetermineDateTimeFormat(input, hasTimeComponent, hasSeconds, hasMilliseconds, hasTimezone);

//            // Preserves original timezone
//            // Converts to UTC for storage while maintaining offset info
//            var success = DateTimeOffset.TryParseExact(
//                input,
//                format,
//                CultureInfo.InvariantCulture,
//                DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces,
//                out var result);

//            if (!success)
//                throw new FormatException($"Failed to parse FHIR date/time: {input}");

//            var precision = hasMilliseconds ? DateTimePrecision.Millisecond :
//                              hasSeconds ? DateTimePrecision.Second :
//                              hasTimeComponent ? DateTimePrecision.Hour :
//                              input.Count(c => c == '-') == 2 ? DateTimePrecision.Day :
//                              input.Contains('-') ? DateTimePrecision.Month :
//                              DateTimePrecision.Year;

//            return (result, precision);
//        }

//        private static string DetermineDateTimeFormat(string input, bool hasTime, bool hasSeconds, bool hasMillis, bool hasTz)
//        {
//            if (!hasTime) return "yyyy-MM-dd";

//            var format = "yyyy-MM-dd'T'HH";
//            if (hasSeconds) format += ":mm:ss";
//            if (hasMillis) format += ".FFFFFFF";
//            if (hasTz)
//            {
//                if (input.EndsWith("Z")) format += "Z";
//                else format += "zzz";
//            }

//            return format;
//        }

//        private static (DateTimeOffset, DateTimePrecision) ParseDate(string input)
//        {
//            var parts = input.Split('-');
//            var year = int.Parse(parts[0]);

//            if (parts.Length == 1)
//                return (new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero), DateTimePrecision.Year);

//            var month = int.Parse(parts[1]);

//            if (parts.Length == 2)
//                return (new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero), DateTimePrecision.Month);

//            var day = int.Parse(parts[2]);
//            return (new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero), DateTimePrecision.Day);
//        }

//        private static (DateTimeOffset, DateTimePrecision) ParseWithPrecision(string input, DateTimePrecision precision)
//        {
//            var success = DateTimeOffset.TryParse(
//                input,
//                CultureInfo.InvariantCulture,
//                DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces,
//                out var result);

//            if (!success)
//                throw new FormatException($"Failed to parse FHIR instant: {input}");

//            return (result, precision);
//        }
//    }
//}
