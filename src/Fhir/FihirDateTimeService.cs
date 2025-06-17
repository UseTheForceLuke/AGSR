using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace FhirService
{
    public enum DateTimePrecision
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second,
        Millisecond
    }

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

    public static class FhirSearchExtensions
    {
        public static IQueryable<T> ApplyDateSearch<T>(
            this IQueryable<T> query,
            FhirSearchParameter searchParam,
            Expression<Func<T, DateTimeOffset>> dateSelector)
        {
            if (!searchParam.Value.HasValue)
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            var dateProperty = Expression.Invoke(dateSelector, parameter);

            var (start, end) = searchParam.GetSearchBounds();

#pragma warning disable CA1308 // Normalize strings to uppercase
            Expression comparison = searchParam.Prefix.ToLowerInvariant() switch
            {
                "eq" => Expression.AndAlso(
                    Expression.GreaterThanOrEqual(dateProperty, Expression.Constant(start)),
                    Expression.LessThanOrEqual(dateProperty, Expression.Constant(end))
                ),
                "ne" => Expression.OrElse(
                    Expression.LessThan(dateProperty, Expression.Constant(start)),
                    Expression.GreaterThan(dateProperty, Expression.Constant(end))
                ),
                "gt" or "sa" => Expression.GreaterThan(dateProperty, Expression.Constant(start)),
                "lt" or "eb" => Expression.LessThan(dateProperty, Expression.Constant(end)),
                "ge" => Expression.GreaterThanOrEqual(dateProperty, Expression.Constant(start)),
                "le" => Expression.LessThanOrEqual(dateProperty, Expression.Constant(end)),
                _ => Expression.AndAlso(
                    Expression.GreaterThanOrEqual(dateProperty, Expression.Constant(start)),
                    Expression.LessThanOrEqual(dateProperty, Expression.Constant(end))
                )
            };
#pragma warning restore CA1308 // Normalize strings to uppercase

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            return query.Where(lambda);
        }
    }

    public class PatientDto
    {
        public int Id { get; set; }
        public DateTimeOffset BirthDate { get; set; }
        public TimeSpan? BirthDateOffset { get; set; }
        public DateTimePrecision BirthDatePrecision { get; set; }
    }
}
