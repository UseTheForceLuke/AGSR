using Application.Abstractions.Messaging;

namespace Application.Patients.GetByBirthDate
{
    public sealed record GetByBirthDateQuery(IList<string> birthDates) : IQuery<IList<PatientResponse>>;
}
