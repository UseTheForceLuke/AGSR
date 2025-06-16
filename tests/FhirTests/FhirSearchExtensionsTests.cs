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

        private readonly IQueryable<TestResource> _testData = new[]
        {
            new TestResource { Id = 1, Date = new DateTimeOffset(1990, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new TestResource { Id = 2, Date = new DateTimeOffset(1990, 6, 15, 12, 0, 0, TimeSpan.Zero) },
            new TestResource { Id = 3, Date = new DateTimeOffset(1990, 12, 31, 23, 59, 59, TimeSpan.Zero) },
            new TestResource { Id = 4, Date = new DateTimeOffset(1991, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new TestResource { Id = 5, Date = new DateTimeOffset(1989, 12, 31, 23, 59, 59, TimeSpan.Zero) }
        }.AsQueryable();

        [Fact]
        public void ApplyDateSearch_EqualsYear_ReturnsMatchingResources()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("eq1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, x => Assert.Equal(1990, x.Date.Year));
        }

        [Fact]
        public void ApplyDateSearch_NotEqualsYear_ReturnsNonMatchingResources()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("ne1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Equal(2, results.Count);
            Assert.Contains(results, x => x.Id == 4); // 1991
            Assert.Contains(results, x => x.Id == 5); // 1989
            Assert.DoesNotContain(results, x => x.Date.Year == 1990);
        }

        [Fact]
        public void ApplyDateSearch_GreaterThanYear_ReturnsLaterResources()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("gt1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(4, results[0].Id); // 1991-01-01
        }

        [Fact]
        public void ApplyDateSearch_LessThanYear_ReturnsEarlierResources()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("lt1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(5, results[0].Id); // 1989-12-31
        }

        [Fact]
        public void ApplyDateSearch_GreaterThanOrEqualYear_ReturnsMatchingAndLaterResources()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("ge1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Equal(4, results.Count);
            Assert.All(results, x => Assert.True(x.Date.Year >= 1990));
        }

        [Fact]
        public void ApplyDateSearch_LessThanOrEqualYear_ReturnsMatchingAndEarlierResources()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("le1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Equal(4, results.Count);
            Assert.All(results, x => Assert.True(x.Date.Year <= 1990));
        }

        [Fact]
        public void ApplyDateSearch_StartsAfter_ReturnsResourcesAfterRange()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("sa1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(4, results[0].Id); // 1991-01-01
        }

        [Fact]
        public void ApplyDateSearch_EndsBefore_ReturnsResourcesBeforeRange()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("eb1990");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(5, results[0].Id); // 1989-12-31
        }

        [Fact]
        public void ApplyDateSearch_WithMonthPrecision_ReturnsCorrectResults()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("eq1990-06");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(2, results[0].Id); // 1990-06-15
        }

        [Fact]
        public void ApplyDateSearch_WithDayPrecision_ReturnsCorrectResults()
        {
            // Arrange
            var searchParam = FhirSearchParameter.Parse("eq1990-06-15");

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(2, results[0].Id); // 1990-06-15
        }

        [Fact]
        public void ApplyDateSearch_WithNullValue_ReturnsOriginalQuery()
        {
            // Arrange
            var searchParam = new FhirSearchParameter { Value = null };
            var expectedCount = _testData.Count();

            // Act
            var results = _testData.ApplyDateSearch(searchParam, x => x.Date).ToList();

            // Assert
            Assert.Equal(expectedCount, results.Count);
        }
    }
}
