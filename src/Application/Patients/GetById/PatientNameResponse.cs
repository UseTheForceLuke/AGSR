namespace Application.Patients.GetById
{
    public sealed record PatientNameResponse(
        Guid Id,
        string? Use,
        string Family,
        List<string>? Given);

    public sealed record PatientResponse(
        PatientNameResponse Name,
        string Gender,
        DateTimeOffset BirthDate,
        bool Active,
        DateTime CreatedAt);
}
