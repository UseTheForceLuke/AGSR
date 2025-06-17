using Application.Abstractions.Messaging;

namespace Application.Patients.Delete
{
    public sealed record DeletePatientsCommand(Guid PatientId) : ICommand;
}
