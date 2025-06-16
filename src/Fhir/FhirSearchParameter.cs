//using FhirService;

//namespace Fhir
//{
//    public class FhirSearchParameter
//    {
//        public string Prefix { get; set; } = "eq";
//        public DateTimeOffset? Value { get; set; }
//        public DateTimePrecision Precision { get; set; }
//        public string OriginalValue { get; set; }

//        public static FhirSearchParameter Parse(string parameter)
//        {
//            if (string.IsNullOrWhiteSpace(parameter))
//                return new FhirSearchParameter();

//            var result = new FhirSearchParameter { OriginalValue = parameter };

//            // Extract prefix
//            var prefixes = new[] { "eq", "ne", "gt", "lt", "ge", "le", "sa", "eb", "ap" };
//            foreach (var prefix in prefixes)
//            {
//                if (parameter.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
//                {
//                    result.Prefix = prefix;
//                    parameter = parameter.Substring(prefix.Length);
//                    break;
//                }
//            }

//            // Parse the date/time value
//            var (parsedDate, precision) = FhirDateTimeParser.ParseFhirDateTime(parameter);
//            result.Value = parsedDate;
//            result.Precision = precision;

//            return result;
//        }

//        public (DateTimeOffset start, DateTimeOffset end) GetSearchBounds()
//        {
//            if (!Value.HasValue)
//                throw new InvalidOperationException("No value to calculate bounds");

//            return Precision switch
//            {
//                DateTimePrecision.Year => (
//                    new DateTimeOffset(Value.Value.Year, 1, 1, 0, 0, 0, Value.Value.Offset),
//                    new DateTimeOffset(Value.Value.Year, 12, 31, 23, 59, 59, 999, Value.Value.Offset)
//                ),
//                DateTimePrecision.Month => (
//                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, 1, 0, 0, 0, Value.Value.Offset),
//                    new DateTimeOffset(Value.Value.Year, Value.Value.Month,
//                        DateTime.DaysInMonth(Value.Value.Year, Value.Value.Month),
//                        23, 59, 59, 999, Value.Value.Offset)
//                ),
//                DateTimePrecision.Day => (
//                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day,
//                        0, 0, 0, Value.Value.Offset),
//                    new DateTimeOffset(Value.Value.Year, Value.Value.Month, Value.Value.Day,
//                        23, 59, 59, 999, Value.Value.Offset)
//                ),
//                DateTimePrecision.Hour => (
//                    Value.Value,
//                    Value.Value.AddHours(1).AddTicks(-1)
//                ),
//                DateTimePrecision.Minute => (
//                    Value.Value,
//                    Value.Value.AddMinutes(1).AddTicks(-1)
//                ),
//                DateTimePrecision.Second => (
//                    Value.Value,
//                    Value.Value.AddSeconds(1).AddTicks(-1)
//                ),
//                _ => (Value.Value, Value.Value)
//            };
//        }

//        public TimeSpan GetApproximateMargin()
//        {
//            return Precision switch
//            {
//                DateTimePrecision.Year => TimeSpan.FromDays(183),  // ~6 months
//                DateTimePrecision.Month => TimeSpan.FromDays(15),  // ~15 days
//                DateTimePrecision.Day => TimeSpan.FromHours(12),
//                DateTimePrecision.Hour => TimeSpan.FromMinutes(30),
//                DateTimePrecision.Minute => TimeSpan.FromSeconds(30),
//                DateTimePrecision.Second => TimeSpan.FromMilliseconds(500),
//                _ => TimeSpan.Zero
//            };
//        }
//    }
//}
