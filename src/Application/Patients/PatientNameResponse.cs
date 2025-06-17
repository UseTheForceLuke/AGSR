namespace Application.Patients
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
        TimeSpan? BirthDateOffset,
        string OriginalBirthDate,
        bool Active,
        DateTime CreatedAt);
}
