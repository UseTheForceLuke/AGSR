using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FhirSeederCLI
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string baseUrl = "http://localhost:5000/patients";
        private static readonly Random random = new Random();

        static async Task Main(string[] args)
        {
            Console.WriteLine("FHIR Patient Data Generator - Hour-Based Timezone Offsets");
            Console.WriteLine("--------------------------------------------------------");

            await GenerateAndPostTestPatients(100);

            Console.WriteLine("\nData generation complete. Press any key to exit.");
            Console.ReadKey();
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
