using Application.Abstractions.Messaging;
using Domain.Patients;

namespace Application.Patients.Update
{
    public sealed class PatientNameDto
    {
        public Guid Id { get; set; }
        public string? Use { get; set; }
        public string? Family { get; set; }
        public List<string>? Given { get; set; }
    }

    public sealed class UpdatePatientsCommand : ICommand<Guid>
    {
        public PatientNameDto Name { get; set; }
        public string? Gender { get; set; }
        public string? BirthDate { get; set; }
        public bool? Active { get; set; }
    }
}
