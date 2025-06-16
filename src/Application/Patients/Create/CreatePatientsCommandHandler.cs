using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Patients.Create;
using Domain.Patients;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Patients.Create
{
    internal sealed class CreatePatientsCommandHandler
        : ICommandHandler<CreatePatientCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreatePatientsCommandHandler(
            IApplicationDbContext context,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<Guid>> Handle(CreatePatientCommand command, CancellationToken cancellationToken)
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                NameUse = command.Name.Use,
                FamilyName = command.Name.Family,
                GivenNames = command.Name.Given,
                Gender = Enum.Parse<Gender>(command.Gender, ignoreCase: true),
                //BirthDate = command.BirthDate,
                Active = command.Active,
                CreatedAt = _dateTimeProvider.UtcNow
            };

            _context.Patients.Add(patient);

            await _context.SaveChangesAsync(cancellationToken);

            return patient.Id;
        }
    }
}
