using SharedKernel;

namespace Domain.Users
{
    public static class PatientErrors
    {
        public static Error NotFound(Guid patientId) => Error.NotFound(
            "Patient.NotFound",
            $"The patient with the Id = '{patientId}' was not found");

        public static Error BirthDateHasInvalidFormat(string birthDate) => Error.Problem(
            "Patient.BirthDateHasInvalidFormat",
            $"Provided {birthDate}' has invalid format");
    }
}
