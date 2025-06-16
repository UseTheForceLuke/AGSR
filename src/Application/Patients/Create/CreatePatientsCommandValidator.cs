using FluentValidation;

namespace Application.Patients.Create
{
    public class CreatePatientsCommandValidator : AbstractValidator<CreatePatientCommand>
    {
        public CreatePatientsCommandValidator()
        {
            RuleFor(c => c.Name).NotNull();
            RuleFor(c => c.Name != null ? c.Name.Family : string.Empty).NotEmpty();
            RuleFor(c => c.BirthDate).NotEmpty();
        }
    }
}
