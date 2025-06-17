using FhirService;
using FhirService.Models;

namespace FhirService
{
    public class FhirSearchParameter
    {
        public string Prefix { get; set; } = "eq";
        public DateTimeOffset? Value { get; set; }
        public TimeSpan? OriginalOffset { get; set; }
        public DateTimePrecision Precision { get; set; }

        public (DateTimeOffset start, DateTimeOffset end) GetSearchBounds()
        {
            if (!Value.HasValue)
                throw new InvalidOperationException("No value to calculate bounds");

            var minDate = new DateTimeOffset(1, 1, 1, 0, 0, 0, Value.Value.Offset);
            var maxDate = new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, Value.Value.Offset);

#pragma warning disable CA1308 // Normalize strings to uppercase
            return (Prefix.ToLowerInvariant(), Precision) switch
            {
                // Equality ranges
                ("eq", DateTimePrecision.Year) => (
                    new DateTimeOffset(Value.Value.Year, 1, 1, 0, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, 12, 31, 23, 59, 59, 999, Value.Value.Offset)
                ),
                ("eq", DateTimePrecision.Month) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, 1, 0, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month,
                        DateTime.DaysInMonth(Value.Value.Year, Value.Value.Month),
                        23, 59, 59, 999, Value.Value.Offset)
                ),
                ("eq", DateTimePrecision.Day) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, 0, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, 23, 59, 59, 999, Value.Value.Offset)
                ),
                ("eq", DateTimePrecision.Hour) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, 59, 59, 999, Value.Value.Offset)
                ),
                ("eq", DateTimePrecision.Minute) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute, 59, 999, Value.Value.Offset)
                ),
                ("eq", DateTimePrecision.Second) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute, Value.Value.Second, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute, Value.Value.Second, 999, Value.Value.Offset)
                ),
                ("eq", DateTimePrecision.Millisecond) =>
                    (Value.Value, Value.Value),

                // Not equals - returns the same range as equality, but the query will be inverted
                ("ne", _) => GetSearchBoundsWithPrefix("eq"),

                // Greater than (returns everything after the period)
                ("gt" or "sa", _) => (
                    GetSearchBoundsWithPrefix("eq").end,
                    maxDate
                ),

                // Less than (returns everything before the period)
                ("lt" or "eb", _) => (
                    minDate,
                    GetSearchBoundsWithPrefix("eq").start
                ),

                // Greater than or equal (returns the period and everything after)
                ("ge", _) => (
                    GetSearchBoundsWithPrefix("eq").start,
                    maxDate
                ),

                // Less than or equal (returns everything up through the period)
                ("le", _) => (
                    minDate,
                    GetSearchBoundsWithPrefix("eq").end
                ),

                // Approximate (adds padding around the value)
                ("ap", DateTimePrecision.Year) => (
                    new DateTimeOffset(Value.Value.Year - 1, 1, 1, 0, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year + 1, 12, 31, 23, 59, 59, 999, Value.Value.Offset)
                ),
                ("ap", DateTimePrecision.Month) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month - 1, 1, 0, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month + 1,
                        DateTime.DaysInMonth(Value.Value.Year, Value.Value.Month + 1),
                        23, 59, 59, 999, Value.Value.Offset)
                ),
                ("ap", DateTimePrecision.Day) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day - 1, 0, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day + 1, 23, 59, 59, 999, Value.Value.Offset)
                ),
                ("ap", DateTimePrecision.Hour) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour - 1, 0, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour + 1, 59, 59, 999, Value.Value.Offset)
                ),
                ("ap", DateTimePrecision.Minute) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute - 1, 0, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute + 1, 59, 999, Value.Value.Offset)
                ),
                ("ap", DateTimePrecision.Second) => (
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute, Value.Value.Second - 1, Value.Value.Offset),
                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Value.Value.Minute, Value.Value.Second + 1, 999, Value.Value.Offset)
                ),
                ("ap", DateTimePrecision.Millisecond) => (
                    Value.Value.AddMilliseconds(-100),
                    Value.Value.AddMilliseconds(100)
                ),

                // Default case (exact match)
                _ => (Value.Value, Value.Value)
            };
#pragma warning restore CA1308 // Normalize strings to uppercase
        }

        private (DateTimeOffset start, DateTimeOffset end) GetSearchBoundsWithPrefix(string prefix)
        {
            var temp = new FhirSearchParameter
            {
                Prefix = prefix,
                Value = Value,
                Precision = Precision
            };
            return temp.GetSearchBounds();
        }

        /// <summary>
        /// Parse is used to initialize FhirSearchParameter, it's like selfcontaied factory method
        /// </summary>
        public static FhirSearchParameter Parse(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return new FhirSearchParameter();

            var result = new FhirSearchParameter();
            var prefixes = new[] { "eq", "ne", "gt", "lt", "ge", "le", "sa", "eb", "ap" };

            foreach (var prefix in prefixes)
            {
                if (parameter.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Prefix = prefix;
                    parameter = parameter[prefix.Length..];
                    break;
                }
            }

            var (parsedDate, originalOffset, precision) = FhirDateTimeParser.ParseFhirDateTime(parameter);
            result.Value = parsedDate;
            result.OriginalOffset = originalOffset;
            result.Precision = precision;

            return result;
        }
    }
}
