using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FhirSeederCLI
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string baseUrl = Environment.GetEnvironmentVariable("WEB_API_URL") + "/patients";
        private static readonly Random random = new Random();

        // Synchronous entry point
        public static int Main(string[] args)
        {
            try
            {
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex}");
                return 1;
            }
        }

        // Async main logic
        public static async Task<int> MainAsync(string[] args)
        {
            if (Environment.GetEnvironmentVariable("WAIT_FOR_API") == "true")
            {
                var healthUrl = Environment.GetEnvironmentVariable("WEB_API_HEALTH_URL");
                var maxRetries = int.Parse(Environment.GetEnvironmentVariable("MAX_RETRIES") ?? "10", CultureInfo.InvariantCulture);
                var retryInterval = int.Parse(Environment.GetEnvironmentVariable("RETRY_INTERVAL") ?? "5", CultureInfo.InvariantCulture);

                if (!await WaitForApi(healthUrl, maxRetries, retryInterval))
                {
                    Console.WriteLine("API did not become healthy in time");
                    return 1;
                }
            }

            Console.WriteLine("FHIR Patient Data Generator");

            await GenerateAndPostTestPatients(100);
            Console.WriteLine("Data generation complete.");
            return 0;
        }

        private static async Task<bool> WaitForApi(string healthUrl, int maxRetries, int retryInterval)
        {
            var retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    var response = await client.GetAsync(healthUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch { }

                retryCount++;
                await Task.Delay(retryInterval * 1000);
            }
            return false;
        }

        private static async Task GenerateAndPostTestPatients(int count)
        {
            Console.WriteLine($"\nGenerating {count} test patients...");

            for (int i = 0; i < count; i++)
            {
                var patient = GeneratePatient();
                var json = JsonSerializer.Serialize(patient, new JsonSerializerOptions
                {
                    IgnoreNullValues = true,
                    WriteIndented = true
                });

                Console.WriteLine($"\nPatient {i + 1} JSON:");
                Console.WriteLine(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(baseUrl, content);

                Console.WriteLine(response.IsSuccessStatusCode
                    ? "Status: Success"
                    : $"Status: Failed ({response.StatusCode})");
            }
        }

        private static dynamic GeneratePatient()
        {
            var format = random.Next(0, 7); // 7 possible formats
            string birthDate;
            var year = random.Next(1950, 2023);
            var month = random.Next(1, 13);
            var day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);

            switch (format)
            {
                case 0: // Hour:Minute:Second
                    birthDate = $"{year}-{month:D2}-{day:D2}T" +
                               $"{random.Next(0, 24):D2}:{random.Next(0, 60):D2}:{random.Next(0, 60):D2}";
                    break;

                case 1: // Hour with timezone (hour-based offset only)
                    birthDate = $"{year}-{month:D2}-{day:D2}T" +
                               $"{random.Next(0, 24):D2}" +
                               $"{GetHourBasedTimezone()}";
                    break;

                case 2: // Hour:Minute with timezone (hour-based offset only)
                    birthDate = $"{year}-{month:D2}-{day:D2}T" +
                                $"{random.Next(0, 24):D2}:{random.Next(0, 60):D2}" +
                                $"{GetHourBasedTimezone()}";
                    break;

                case 3: // Hour:Minute:Second with timezone (hour-based offset only)
                    birthDate = $"{year}-{month:D2}-{day:D2}T" +
                               $"{random.Next(0, 24):D2}:{random.Next(0, 60):D2}:{random.Next(0, 60):D2}" +
                               $"{GetHourBasedTimezone()}";
                    break;

                case 4: // Year only
                    birthDate = $"{year}";
                    break;

                case 5: // Year-Month
                    birthDate = $"{year}-{month:D2}";
                    break;

                case 6: // Year-Month-Day
                default:
                    birthDate = $"{year}-{month:D2}-{day:D2}";
                    break;
            }

            return new
            {
                name = new
                {
                    family = LastNames[random.Next(LastNames.Length)],
                    given = new[] { FirstNames[random.Next(FirstNames.Length)] }
                },
                gender = Genders[random.Next(Genders.Length)],
                birthDate = birthDate,
                active = random.NextDouble() > 0.2 // 80% active
            };
        }

        private static string GetHourBasedTimezone()
        {
            switch (random.Next(0, 4))
            {
                case 0:
                    return "Z";  // UTC
                case 1:
                    return $"+{random.Next(0, 15):D2}:00";  // Positive offset (hours only)
                case 2:
                    return $"-{random.Next(0, 12):D2}:00";  // Negative offset (hours only)
                default:
                    return null; // No timezone
            }
        }

        private static readonly string[] FirstNames = { "James", "Mary", "John", "Patricia", "Robert", "Jennifer" };
        private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller" };
        private static readonly string[] Genders = { "male", "female", "other", "unknown" };
    }
}
