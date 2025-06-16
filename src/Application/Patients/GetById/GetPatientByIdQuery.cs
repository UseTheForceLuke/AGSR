using Application.Abstractions.Messaging;
using Domain.Patients;

namespace Application.Patients.GetById
{
    public sealed record GetPatientByIdQuery(Guid PatientId) : IQuery<PatientResponse>;
}
