using FhirService;
using FhirService.Models;

namespace FhirServiceTests
{
    public class FhirServiceTests
    {
        [Theory]
        [InlineData("1990", 1990, 1, 1, 0, 0, 0, null, DateTimePrecision.Year)]
        [InlineData("1990-06", 1990, 6, 1, 0, 0, 0, null, DateTimePrecision.Month)]
        [InlineData("1990-06-15", 1990, 6, 15, 0, 0, 0, null, DateTimePrecision.Day)]
        [InlineData("1990-06-15T14", 1990, 6, 15, 14, 0, 0, null, DateTimePrecision.Hour)]
        [InlineData("1990-06-15T14:30", 1990, 6, 15, 14, 30, 0, null, DateTimePrecision.Minute)]
        [InlineData("1990-06-15T14:30:45", 1990, 6, 15, 14, 30, 45, null, DateTimePrecision.Second)]
        [InlineData("1990-06-15T14:30:45.123", 1990, 6, 15, 14, 30, 45, null, DateTimePrecision.Millisecond)]
        [InlineData("1990-06-15T14:30:45Z", 1990, 6, 15, 14, 30, 45, "00:00:00", DateTimePrecision.Second)]
        [InlineData("1990-06-15T14:30:45+02:00", 1990, 6, 15, 12, 30, 45, "02:00:00", DateTimePrecision.Second)]
        [InlineData("1990-06-15T14:30:45-05:00", 1990, 6, 15, 19, 30, 45, "-05:00:00", DateTimePrecision.Second)]
        [InlineData("1990-06-15T14:30:45+03:30", 1990, 6, 15, 11, 0, 45, "03:30:00", DateTimePrecision.Second)]
        public void ParseFhirDateTime_ValidInput_ReturnsCorrectResult(
            string input,
            int year,
            int month,
            int day,
            int hour,
            int minute,
            int second,
            string expectedOffsetString,
            DateTimePrecision expectedPrecision)
        {
            // Act
            var (parsedDate, originalOffset, precision) = FhirDateTimeParser.ParseFhirDateTime(input);

            // Assert - check parsed UTC values
            Assert.Equal(expectedPrecision, precision);
            Assert.Equal(year, parsedDate?.Year);
            Assert.Equal(month, parsedDate?.Month);
            Assert.Equal(day, parsedDate?.Day);
            Assert.Equal(hour, parsedDate?.Hour);
            Assert.Equal(minute, parsedDate?.Minute);
            Assert.Equal(second, parsedDate?.Second);

            // Assert - check offset
            if (expectedOffsetString == null)
            {
                Assert.Null(originalOffset);
            }
            else
            {
                Assert.NotNull(originalOffset);
                var expectedOffset = TimeSpan.Parse(expectedOffsetString);
                Assert.Equal(expectedOffset, originalOffset);
            }

            // Additional check - verify that when we apply the original offset, we get back to the local time
            if (originalOffset.HasValue)
            {
                var localTime = parsedDate.Value.ToOffset(originalOffset.Value);

                // Extract the hour from the input string more safely
                int hourInInput = 0;
                var timePart = input.Split('T').LastOrDefault()?.Split(new[] { '+', '-', 'Z' }).FirstOrDefault();
                if (timePart != null && timePart.Contains(':'))
                {
                    var hourStr = timePart.Split(':')[0];
                    if (int.TryParse(hourStr, out var parsedHour))
                    {
                        hourInInput = parsedHour;
                    }
                }

                Assert.Equal(hourInInput, localTime.Hour);
            }
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("1990-13-01")] // Invalid month
        [InlineData("1990-06-32")] // Invalid day
        [InlineData("1990-06-15T25:00")] // Invalid hour
        public void ParseFhirDateTime_InvalidInput_ThrowsFormatException(string input)
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => FhirDateTimeParser.ParseFhirDateTime(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseFhirDateTime_InvalidInput_ThrowsArgumentNullExceptionn(string input)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => FhirDateTimeParser.ParseFhirDateTime(input));
        }


        #region GetSearchBounds

        [Theory]
        // Equality tests
        [InlineData("1990", "eq", 1990, 1, 1, 1990, 12, 31)]           // Full year range
        [InlineData("1990-06", "eq", 1990, 6, 1, 1990, 6, 30)]         // Full month range (June)
        [InlineData("1990-02", "eq", 1990, 2, 1, 1990, 2, 28)]         // February (non-leap year)
        [InlineData("2024-02", "eq", 2024, 2, 1, 2024, 2, 29)]         // February (leap year)
        [InlineData("1990-06-15", "eq", 1990, 6, 15, 1990, 6, 15)]     // Exact day
        // Greater than tests
        [InlineData("1990", "gt", 1990, 12, 31, 9999, 12, 31)]         // After end of year
        [InlineData("1990-06", "gt", 1990, 6, 30, 9999, 12, 31)]       // After end of month
        [InlineData("1990-06-15", "gt", 1990, 6, 15, 9999, 12, 31)]    // After exact day
        // Less than tests
        [InlineData("1990", "lt", 1, 1, 1, 1990, 1, 1)]                // Before start of year
        [InlineData("1990-06", "lt", 1, 1, 1, 1990, 6, 1)]             // Before start of month
        [InlineData("1990-06-15", "lt", 1, 1, 1, 1990, 6, 15)]         // Before exact day
        // Greater than or equal
        [InlineData("1990", "ge", 1990, 1, 1, 9999, 12, 31)]           // Year and after
        [InlineData("1990-06-15", "ge", 1990, 6, 15, 9999, 12, 31)]    // Day and after
        // Less than or equal
        [InlineData("1990", "le", 1, 1, 1, 1990, 12, 31)]              // Up to end of year
        [InlineData("1990-06-15", "le", 1, 1, 1, 1990, 6, 15)]         // Up to exact day
        public void GetSearchBounds_ReturnsCorrectRanges(
            string input, string prefix, int startYear, int startMonth, int startDay,
            int endYear, int endMonth, int endDay)
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse(prefix + input);

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(startYear, start.Year);
            Assert.Equal(startMonth, start.Month);
            Assert.Equal(startDay, start.Day);

            Assert.Equal(endYear, end.Year);
            Assert.Equal(endMonth, end.Month);
            Assert.Equal(endDay, end.Day);
        }

        #region Time
        // Helper method to create a search parameter with the given prefix and datetime string
        private FhirSearchParameter CreateSearchParam(string prefix, string dateTimeStr)
        {
            var param = FhirSearchParameter.Parse(prefix + dateTimeStr);
            return param;
        }

        [Fact]
        public void GetSearchBounds_EqualsYear_ReturnsFullYearRange()
        {
            // Arrange
            var searchParam = CreateSearchParam("eq", "2023");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 12, 31, 23, 59, 59, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_EqualsMonth_ReturnsFullMonthRange()
        {
            // Arrange
            var searchParam = CreateSearchParam("eq", "2023-02");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 1, 0, 0, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 28, 23, 59, 59, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_EqualsDay_ReturnsFullDayRange()
        {
            // Arrange
            var searchParam = CreateSearchParam("eq", "2023-02-15");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 0, 0, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 23, 59, 59, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_EqualsHour_ReturnsFullHourRange()
        {
            // Arrange
            var searchParam = CreateSearchParam("eq", "2023-02-15T14");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 0, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 59, 59, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_EqualsMinute_ReturnsFullMinuteRange()
        {
            // Arrange
            var searchParam = CreateSearchParam("eq", "2023-02-15T14:30");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 59, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_EqualsSecond_ReturnsFullSecondRange()
        {
            // Arrange
            var searchParam = CreateSearchParam("eq", "2023-02-15T14:30:45");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 45, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 45, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_EqualsMillisecond_ReturnsExactPoint()
        {
            // Arrange
            var searchParam = CreateSearchParam("eq", "2023-02-15T14:30:45.123");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 45, 123, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 45, 123, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_GreaterThanHour_ReturnsAfterHour()
        {
            // Arrange
            var searchParam = CreateSearchParam("gt", "2023-02-15T14");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 59, 59, 999, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_LessThanMinute_ReturnsBeforeMinute()
        {
            // Arrange
            var searchParam = CreateSearchParam("lt", "2023-02-15T14:30");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 0, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_GreaterThanOrEqualSecond_ReturnsFromSecondOnwards()
        {
            // Arrange
            var searchParam = CreateSearchParam("ge", "2023-02-15T14:30:45");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 45, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_LessThanOrEqualMillisecond_ReturnsUpToMillisecond()
        {
            // Arrange
            var searchParam = CreateSearchParam("le", "2023-02-15T14:30:45.123");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 45, 123, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_EndsBeforeSecond_ReturnsBeforeExactPoint()
        {
            // Arrange
            var searchParam = CreateSearchParam("eb", "2023-02-15T14:30:45");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            Assert.Equal(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero), start);
            Assert.Equal(new DateTimeOffset(2023, 2, 15, 14, 30, 45, TimeSpan.Zero), end);
        }

        [Fact]
        public void GetSearchBounds_ApproximateDay_ReturnsDayRangeWithPadding()
        {
            // Arrange
            var searchParam = CreateSearchParam("ap", "2023-02-15");

            // Act
            var (start, end) = searchParam.GetSearchBounds();

            // Assert
            // Approximate should add some padding - here we're testing it returns at least the exact day
            Assert.True(start <= new DateTimeOffset(2023, 2, 15, 0, 0, 0, TimeSpan.Zero));
            Assert.True(end >= new DateTimeOffset(2023, 2, 15, 23, 59, 59, 999, TimeSpan.Zero));
        }

        #endregion
        #endregion
    }
}
