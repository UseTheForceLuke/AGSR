namespace FhirService.Models
{
    public class PatientDto
    {
        public int Id { get; set; }
        public DateTimeOffset BirthDate { get; set; }
        public TimeSpan? BirthDateOffset { get; set; }
        public DateTimePrecision BirthDatePrecision { get; set; }
    }
}
