using Application.Patients.Update;
using FluentValidation;

namespace Application.Patients.Create
{
    public class UpdatePatientsCommandValidator : AbstractValidator<UpdatePatientsCommand>
    {
        public UpdatePatientsCommandValidator()
        {
            RuleFor(c => c.Name).NotEmpty();
        }
    }
}
