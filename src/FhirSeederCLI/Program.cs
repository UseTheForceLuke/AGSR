using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Bogus;
using Dapper;
using Npgsql;
using Polly;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Collections;

namespace FhirPatientSeeder
{
    class Program
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Database=postgress-agsr-db;Username=postgres;Password=postgres;Include Error Detail=true";
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);

        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("FHIR Patient Seeder with Database Polling");

            var countOption = new Option<int>(
                new[] { "--count", "-c" },
                () => 100,
                "Number of patients to generate");

            var timeoutOption = new Option<int>(
                new[] { "--timeout", "-t" },
                () => 120,
                "Maximum wait time in seconds for database to be ready");

            rootCommand.AddOption(countOption);
            rootCommand.AddOption(timeoutOption);

            rootCommand.Handler = CommandHandler.Create<int, int>(async (count, timeout) =>
            {
                await RunSeeder(count, timeout);
            });

            await rootCommand.InvokeAsync(args);
        }

        private static async Task RunSeeder(int count, int timeoutSeconds)
        {
            try
            {
                var dbService = new DatabaseService(ConnectionString);
                var maxWaitTime = TimeSpan.FromSeconds(timeoutSeconds);
                var startTime = DateTime.UtcNow;
                bool isReady = false;

                Console.WriteLine($"Waiting for database to be ready (max {maxWaitTime.TotalSeconds} seconds)...");

                while (!isReady && DateTime.UtcNow - startTime < maxWaitTime)
                {
                    try
                    {
                        isReady = await dbService.VerifyDatabaseReady();
                        if (!isReady)
                        {
                            Console.WriteLine("Database not ready yet. Waiting...");
                            await Task.Delay(InitialDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Database check failed: {ex.Message}");
                        await Task.Delay(InitialDelay);
                    }
                }

                if (!isReady)
                {
                    Console.WriteLine("Timeout reached. Database not ready.");
                    return;
                }

                Console.WriteLine("Database verified. Generating patients...");
                var patients = PatientGenerator.GeneratePatients(count);

                Console.WriteLine($"Generated {patients.Count} patients. Sample:");
                foreach (var p in patients.GetRange(0, Math.Min(3, patients.Count)))
                {
                    Console.WriteLine($"- {p.FamilyName}, {string.Join(" ", p.GivenNames)} | " +
                                    $"Gender: {p.Gender} | DOB: {p.BirthDate:yyyy-MM-dd}");
                }

                Console.WriteLine("Seeding database...");
                await dbService.SeedPatients(patients);

                Console.WriteLine("Seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public enum Gender { Male, Female, Other, Unknown }

    public class Patient
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NameUse { get; set; }
        public string FamilyName { get; set; }
        public List<string> GivenNames { get; set; }
        public Gender Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> VerifyDatabaseReady()
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(6, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            try
            {
                return await policy.ExecuteAsync(async () =>
                {
                    using (var db = new NpgsqlConnection(_connectionString))
                    {
                        await db.OpenAsync();
                        return await DoesTableExist(db, "patients");
                    }
                });
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> DoesTableExist(IDbConnection db, string tableName)
        {
            var result = await db.QueryFirstOrDefaultAsync<int>(
                @"SELECT 1 FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = 'patients'",
                new { tableName = tableName.ToLower() });

            return result == 1;
        }

        public static class JsonHelper
        {
            public static string ToJson(object value)
            {
                // Simple JSON serializer for .NET Core 3.1
                if (value == null) return "null";
                if (value is string str) return $"\"{str.Replace("\"", "\\\"")}\"";
                if (value is IEnumerable enumerable)
                {
                    var items = new List<string>();
                    foreach (var item in enumerable)
                    {
                        items.Add(ToJson(item));
                    }
                    return $"[{string.Join(",", items)}]";
                }
                return value.ToString().ToLower(); // For booleans
            }
        }

        public async Task SeedPatients(IEnumerable<Patient> patients)
        {
            using (var db = new NpgsqlConnection(_connectionString))
            {
                await db.OpenAsync();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        foreach (var patient in patients)
                        {
                            await db.ExecuteAsync(@"
                        INSERT INTO patients 
                        (id, name_use, family_name, given_names, gender, birth_date, active, created_at)
                        VALUES 
                        (@Id, @NameUse, @FamilyName, @GivenNames::jsonb, @Gender, @BirthDate, @Active, @CreatedAt)",
                                new
                                {
                                    patient.Id,
                                    patient.NameUse,
                                    patient.FamilyName,
                                    GivenNames = JsonHelper.ToJson(patient.GivenNames),
                                    Gender = patient.Gender.ToString(),
                                    patient.BirthDate,
                                    patient.Active,
                                    patient.CreatedAt
                                },
                                transaction);
                        }
                        transaction.Commit();
                        Console.WriteLine($"Successfully seeded {patients.Count()} patients");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Database error: {ex.Message}");
                        throw;
                    }
                }
            }
        }
    }

    public static class PatientGenerator
    {
        public static List<Patient> GeneratePatients(int count)
        {
            var genders = new[] { "male", "female", "other", "unknown" };

            return new Faker<Patient>()
                .RuleFor(p => p.NameUse, f => f.PickRandom("official", "usual", "nickname", "anonymous"))
                .RuleFor(p => p.FamilyName, f => f.Name.LastName())
                .RuleFor(p => p.GivenNames, f => new List<string> { f.Name.FirstName(), f.Name.FirstName() })
                .RuleFor(p => p.Gender, f => (Gender)Array.IndexOf(genders, f.PickRandom(genders)))
                .RuleFor(p => p.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-100), DateTime.Now.AddYears(-18)))
                .RuleFor(p => p.Active, f => f.Random.Bool(0.9f))
                .Generate(count);
        }
    }
}
