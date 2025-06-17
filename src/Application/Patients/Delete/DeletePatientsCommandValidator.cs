using FluentValidation;

namespace Application.Patients.Delete
{
    internal sealed class DeletePatientsCommandValidator : AbstractValidator<DeletePatientsCommand>
    {
        public DeletePatientsCommandValidator()
        {
        }
    }
}
