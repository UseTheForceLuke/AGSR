using FhirService;

namespace FhirServiceTests
{
    public class FhirSearchExtensionsTests
    {
        public class TestResource
        {
            public DateTimeOffset Date { get; set; }
            public int Id { get; set; }
        }

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

        [Fact]
        public void ParseFhirDateTime_WithZuluTimeZone_ReturnsCorrectOffset()
        {
            // Arrange
            var input = "2020-06-15T12:30:45Z";

            // Act
            var (parsedDate, originalOffset, precision) = FhirDateTimeParser.ParseFhirDateTime(input);

            // Assert
            Assert.NotNull(parsedDate);
            Assert.Equal(TimeSpan.Zero, originalOffset);
            Assert.Equal(DateTimePrecision.Second, precision);
            Assert.Equal(TimeSpan.Zero, parsedDate.Value.Offset);
            Assert.Equal(2020, parsedDate.Value.Year);
            Assert.Equal(6, parsedDate.Value.Month);
            Assert.Equal(15, parsedDate.Value.Day);
            Assert.Equal(12, parsedDate.Value.Hour);
            Assert.Equal(30, parsedDate.Value.Minute);
            Assert.Equal(45, parsedDate.Value.Second);
        }

        [Fact]
        public void ParseFhirDateTime_WithPositiveOffset_ReturnsCorrectOffset()
        {
            // Arrange
            var input = "2020-06-15T12:30:45+05:30";

            // Act
            var (parsedDate, originalOffset, precision) = FhirDateTimeParser.ParseFhirDateTime(input);

            // Assert
            Assert.NotNull(parsedDate);
            Assert.Equal(TimeSpan.FromHours(5.5), originalOffset);
            Assert.Equal(DateTimePrecision.Second, precision);
            Assert.Equal(TimeSpan.Zero, parsedDate.Value.Offset); // Should be converted to UTC
            Assert.Equal(7, parsedDate.Value.Hour); // 12:30 - 5:30 = 7:00 UTC
        }

        [Fact]
        public void ParseFhirDateTime_WithNegativeOffset_ReturnsCorrectOffset()
        {
            // Arrange
            var input = "2020-06-15T12:30:45-03:00";

            // Act
            var (parsedDate, originalOffset, precision) = FhirDateTimeParser.ParseFhirDateTime(input);

            // Assert
            Assert.NotNull(parsedDate);
            Assert.Equal(TimeSpan.FromHours(-3), originalOffset);
            Assert.Equal(DateTimePrecision.Second, precision);
            Assert.Equal(TimeSpan.Zero, parsedDate.Value.Offset); // Should be converted to UTC
            Assert.Equal(15, parsedDate.Value.Hour); // 12:30 + 3:00 = 15:30 UTC
        }

        [Fact]
        public void ApplyDateSearch_WithTimeZone_ReturnsCorrectResults()
        {
            // Arrange
            var testPatients = new List<Patient>
            {
                // UTC times
                new Patient { Id = 1, BirthDate = new DateTimeOffset(2020, 6, 15, 7, 0, 0, TimeSpan.Zero) },  // Equivalent to 12:30+05:30
                new Patient { Id = 2, BirthDate = new DateTimeOffset(2020, 6, 15, 15, 30, 0, TimeSpan.Zero) }, // Equivalent to 12:30-03:00
                new Patient { Id = 3, BirthDate = new DateTimeOffset(2020, 6, 15, 12, 30, 0, TimeSpan.Zero) }  // UTC
            }.AsQueryable();

            // Search for 12:30 in +05:30 timezone (which is 07:00 UTC)
            var searchParam = FhirSearchParameter.Parse("eq2020-06-15T12:30+05:30");

            // Act
            var results = testPatients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(1, results[0].Id); // Should match the first patient which is 07:00 UTC
        }

        [Fact]
        public void ApplyDateSearch_WithDifferentTimeZones_ComparesCorrectly()
        {
            // Arrange
            var testPatients = new List<Patient>
            {
                // All represent the same moment in time in different timezones
                new Patient { Id = 1, BirthDate = new DateTimeOffset(2020, 6, 15, 12, 30, 0, TimeSpan.FromHours(2)) },  // 12:30+02:00 (10:30 UTC)
                new Patient { Id = 2, BirthDate = new DateTimeOffset(2020, 6, 15, 5, 30, 0, TimeSpan.FromHours(-5)) },   // 05:30-05:00 (10:30 UTC)
                new Patient { Id = 3, BirthDate = new DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero) }           // 10:30 UTC
            }.AsQueryable();

            // Search for 12:30 in +02:00 timezone (which is 10:30 UTC)
            var searchParam = FhirSearchParameter.Parse("eq2020-06-15T12:30+02:00");

            // Act
            var results = testPatients.ApplyDateSearch(searchParam, p => p.BirthDate).ToList();

            // Assert - all should match since they represent the same moment
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void ParseFhirDateTime_WithMaxOffset_ReturnsCorrectOffset()
        {
            // Arrange
            var input = "2020-06-15T12:30:45+14:00";

            // Act
            var (parsedDate, originalOffset, precision) = FhirDateTimeParser.ParseFhirDateTime(input);

            // Assert
            Assert.NotNull(parsedDate);
            Assert.Equal(TimeSpan.FromHours(14), originalOffset);
            Assert.Equal(DateTimePrecision.Second, precision);
            Assert.Equal(TimeSpan.Zero, parsedDate.Value.Offset); // Should be converted to UTC
            Assert.Equal(2020, parsedDate.Value.Year);
            Assert.Equal(6, parsedDate.Value.Month);
            Assert.Equal(14, parsedDate.Value.Day); // 12:30 + 14:00 = 26:30 which rolls over to next day
            Assert.Equal(22, parsedDate.Value.Hour); // 26:30 - 24 = 2:30, but 12:30 - 14:00 is actually -1:30 (previous day 22:30)
        }

        [Fact]
        public void ParseFhirDateTime_WithMinOffset_ReturnsCorrectOffset()
        {
            // Arrange
            var input = "2020-06-15T12:30:45-12:00";

            // Act
            var (parsedDate, originalOffset, precision) = FhirDateTimeParser.ParseFhirDateTime(input);

            // Assert
            Assert.NotNull(parsedDate);
            Assert.Equal(TimeSpan.FromHours(-12), originalOffset);
            Assert.Equal(DateTimePrecision.Second, precision);
            Assert.Equal(TimeSpan.Zero, parsedDate.Value.Offset); // Should be converted to UTC
            Assert.Equal(2020, parsedDate.Value.Year);
            Assert.Equal(6, parsedDate.Value.Month);
            Assert.Equal(16, parsedDate.Value.Day); // 12:30 + 12:00 = 24:30 which rolls over to next day
            Assert.Equal(0, parsedDate.Value.Hour); // 24:30 - 24 = 0:30
            Assert.Equal(30, parsedDate.Value.Minute);
        }

        #endregion
    }
}
