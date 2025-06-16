using Application.Patients.GetByBirthDate;
using FluentValidation;

namespace Application.Patients.Create
{
    public class GetByBirthDateQueryValidator : AbstractValidator<GetByBirthDateQuery>
    {
        public GetByBirthDateQueryValidator()
        {
        }
    }
}
