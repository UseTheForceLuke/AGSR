using FhirService;

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

        #region ApplyDateSearch

        private readonly IQueryable<Patient> _patients = new List<Patient>
        {
            new Patient { Id = 1, BirthDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new Patient { Id = 2, BirthDate = new DateTimeOffset(2020, 6, 15, 0, 0, 0, TimeSpan.Zero) },
            new Patient { Id = 3, BirthDate = new DateTimeOffset(2020, 6, 15, 12, 0, 0, TimeSpan.Zero) },
            new Patient { Id = 4, BirthDate = new DateTimeOffset(2020, 6, 15, 12, 30, 0, TimeSpan.Zero) },
            new Patient { Id = 5, BirthDate = new DateTimeOffset(2020, 6, 15, 12, 30, 45, TimeSpan.Zero) },
            new Patient { Id = 6, BirthDate = new DateTimeOffset(2020, 12, 31, 23, 59, 59, TimeSpan.Zero) },
            new Patient { Id = 7, BirthDate = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero) }
        }.AsQueryable();

        [Fact]
        public void ApplyDateSearch_EqYear_ReturnsMatchingYear()
        {
            var searchParam = FhirSearchParameter.Parse("eq2020");
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Equal(6, results.Count);
            Assert.All(results, p => Assert.Equal(2020, p.BirthDate.Year));
        }

        [Fact]
        public void ApplyDateSearch_NeYear_ReturnsNonMatchingYears()
        {
            var searchParam = FhirSearchParameter.Parse("ne2020");
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Single(results);
            Assert.Equal(2021, results[0].BirthDate.Year);
        }

        [Fact]
        public void ApplyDateSearch_GtMonth_ReturnsAfterMonth()
        {
            var searchParam = FhirSearchParameter.Parse("gt2020-06");
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Equal(2, results.Count);
            Assert.All(results, p => Assert.True(p.BirthDate > new DateTimeOffset(2020, 6, 30, 23, 59, 59, TimeSpan.Zero)));
        }

        [Fact]
        public void ApplyDateSearch_LtDay_ReturnsBeforeDay()
        {
            var searchParam = FhirSearchParameter.Parse("lt2020-06-15");
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Single(results);
            Assert.Equal(1, results[0].Id);
        }

        [Fact]
        public void ApplyDateSearch_GeHour_ReturnsHourAndAfter4()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("ge2020-06-15T12");

            // Act
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate)
                                  .OrderBy(p => p.BirthDate)
                                  .ToList();

            // Assert
            Assert.Equal(5, results.Count);
            Assert.Equal(3, results[0].Id); // 12:00
            Assert.Equal(4, results[1].Id); // 12:30
            Assert.Equal(5, results[2].Id); // 12:30:45
            Assert.Equal(6, results[3].Id); // Dec 31
            Assert.Equal(7, results[4].Id); // Jan 1 2021
        }

        [Fact]
        public void ApplyDateSearch_GeHour_ReturnsHourAndAfter()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("ge2020-06-15T12");

            // Act
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();

            // Assert - Should include:
            // - Exactly 12:00 (Id 3)
            // - After 12:00 same day (Id 4,5)
            // - All times on later dates (Id 6,7)
            Assert.Equal(5, results.Count);
            Assert.Contains(results, p => p.Id == 3);
            Assert.Contains(results, p => p.Id == 4);
            Assert.Contains(results, p => p.Id == 5);
            Assert.Contains(results, p => p.Id == 6);
            Assert.Contains(results, p => p.Id == 7);
        }


        [Fact]
        public void ApplyDateSearch_LeMinute_ReturnsUpToMinute()
        {
            var searchParam = FhirSearchParameter.Parse("le2020-06-15T12:30");
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Equal(5, results.Count);
            Assert.All(results, p => Assert.True(p.BirthDate <= new DateTimeOffset(2020, 6, 15, 12, 30, 59, 999, TimeSpan.Zero)));
        }

        [Fact]
        public void ApplyDateSearch_SaSecond_ReturnsAfterSecond()
        {
            var searchParam = FhirSearchParameter.Parse("sa2020-06-15T12:30:45");
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Equal(2, results.Count);
            Assert.All(results, p => Assert.True(p.BirthDate > new DateTimeOffset(2020, 6, 15, 12, 30, 45, 999, TimeSpan.Zero)));
        }

        [Fact]
        public void ApplyDateSearch_EbDay_ReturnsBeforeDay()
        {
            var searchParam = FhirSearchParameter.Parse("eb2020-06-15");
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Single(results);
            Assert.Equal(1, results[0].Id);
        }

        [Fact]
        public void ApplyDateSearch_ApMonth_ReturnsApproximateMonth3()
        {
            // Arrange
            var testPatients = new List<Patient>
            {
                new Patient { Id = 1, BirthDate = new DateTimeOffset(2020, 4, 30, 23, 59, 59, TimeSpan.Zero) },
                new Patient { Id = 2, BirthDate = new DateTimeOffset(2020, 5, 1, 0, 0, 0, TimeSpan.Zero) },
                new Patient { Id = 3, BirthDate = new DateTimeOffset(2020, 5, 31, 23, 59, 59, TimeSpan.Zero) },
                new Patient { Id = 4, BirthDate = new DateTimeOffset(2020, 6, 1, 0, 0, 0, TimeSpan.Zero) },
                new Patient { Id = 5, BirthDate = new DateTimeOffset(2020, 6, 30, 23, 59, 59, TimeSpan.Zero) },
                new Patient { Id = 6, BirthDate = new DateTimeOffset(2020, 7, 1, 0, 0, 0, TimeSpan.Zero) },
                new Patient { Id = 7, BirthDate = new DateTimeOffset(2020, 7, 31, 23, 59, 59, TimeSpan.Zero) },
                new Patient { Id = 8, BirthDate = new DateTimeOffset(2020, 8, 1, 0, 0, 0, TimeSpan.Zero) }
            }.AsQueryable();

            var searchParam = FhirSearchParameter.Parse("ap2020-06");

            // Act
            var results = testPatients.ApplyDateSearch(searchParam, p => p.BirthDate)
                                     .OrderBy(p => p.BirthDate)
                                     .ToList();

            // Assert - Should include May 1 through July 31 (6 records)
            Assert.Equal(6, results.Count); // Updated expectation
            Assert.Equal(2, results[0].Id); // May 1
            Assert.Equal(3, results[1].Id); // May 31
            Assert.Equal(4, results[2].Id); // June 1
            Assert.Equal(5, results[3].Id); // June 30
            Assert.Equal(6, results[4].Id); // July 1
            Assert.Equal(7, results[5].Id); // July 31
        }

        [Fact]
        public void ApplyDateSearch_ApMonth_HandlesLeapYear()
        {
            var testPatients = new List<Patient>
            {
                new Patient { Id = 1, BirthDate = new DateTimeOffset(2020, 2, 28, 0, 0, 0, TimeSpan.Zero) },
                new Patient { Id = 2, BirthDate = new DateTimeOffset(2020, 2, 29, 0, 0, 0, TimeSpan.Zero) },
                new Patient { Id = 3, BirthDate = new DateTimeOffset(2020, 3, 1, 0, 0, 0, TimeSpan.Zero) }
            }.AsQueryable();

            var searchParam = FhirSearchParameter.Parse("ap2020-02");
            var results = testPatients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();

            Assert.Equal(3, results.Count); // All should be included
        }

        [Fact]
        public void ApplyDateSearch_EqMillisecond_ReturnsExactMatch()
        {
            var testDate = new DateTimeOffset(2020, 6, 15, 12, 30, 45, 500, TimeSpan.Zero);
            var patients = _patients.Concat(new[] { new Patient { Id = 8, BirthDate = testDate } });

            var searchParam = new FhirSearchParameter
            {
                Prefix = "eq",
                Value = testDate,
                Precision = DateTimePrecision.Millisecond
            };

            var results = patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Single(results);
            Assert.Equal(8, results[0].Id);
        }

        [Fact]
        public void ApplyDateSearch_EmptyValue_ReturnsAllPatients()
        {
            var searchParam = new FhirSearchParameter();
            var results = _patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();
            Assert.Equal(7, results.Count);
        }

        [Fact]
        public void ApplyDateSearch_EqualsYear_ReturnsMatchingPatients()
        {
            // Arrange
            var patients = new[]
            {
            new Patient { BirthDate = new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero), BirthDatePrecision = DateTimePrecision.Year },
            new Patient { BirthDate = new DateTimeOffset(1991, 1, 1, 0, 0, 0, TimeSpan.Zero), BirthDatePrecision = DateTimePrecision.Year }
        }.AsQueryable();

            var searchParam = FhirSearchParameter.Parse("eq1990");

            // Act
            var results = patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(1990, results[0].BirthDate.Year);
        }

        [Fact]
        public void ApplyDateSearch_GreaterThanMonth_ReturnsLaterPatients()
        {
            // Arrange
            var patients = new[]
            {
            new Patient { BirthDate = new DateTimeOffset(1990, 5, 1, 0, 0, 0, TimeSpan.Zero) },
            new Patient { BirthDate = new DateTimeOffset(1990, 7, 1, 0, 0, 0, TimeSpan.Zero) }
        }.AsQueryable();

            var searchParam = FhirSearchParameter.Parse("gt1990-06");

            // Act
            var results = patients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(7, results[0].BirthDate.Month);
        }

        #endregion
    }
}
