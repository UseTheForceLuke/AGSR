using System.ComponentModel.DataAnnotations;
using SharedKernel;

namespace Domain.Patients
{
    public class Patient : Entity
    {
        public Guid Id { get; set; }

        public string? NameUse { get; set; }
        public string FamilyName { get; set; }
        public List<string>? GivenNames { get; set; }

        public Gender Gender { get; set; } = Gender.Unknown;
        public DateTimeOffset BirthDate { get; set; }
        public TimeSpan? BirthDateOffset { get; set; }
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; }

    }

    public enum Gender
    {
        Unknown = 0,
        Male = 1,
        Female = 2,
        Other = 3
    }
}
