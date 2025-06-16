using Application.Abstractions.Messaging;
using Domain.Patients;

namespace Application.Patients.GetByBirthDate
{
    public sealed record GetByBirthDateQuery(IList<string> birthDates) : IQuery<IList<PatientResponse>>;
}
