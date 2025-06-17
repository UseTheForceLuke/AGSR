using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Patients.Update;
using Domain.Patients;
using FhirService;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Patients.Create
{
    internal sealed class UpdatePatientsCommandHandler
        : ICommandHandler<UpdatePatientsCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdatePatientsCommandHandler(
            IApplicationDbContext context,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<Guid>> Handle(UpdatePatientsCommand command, CancellationToken cancellationToken)
        {

            Patient? patient = await _context.Patients.FirstOrDefaultAsync(x => x.Id == command.Name.Id, cancellationToken);

            if(patient is null)
            {
                return Result.Failure<Guid>(PatientErrors.NotFound(command.Name.Id));
            }

            (DateTimeOffset? parsedDate, TimeSpan? originalOffset, DateTimePrecision precision)? result = default;
            if (command.BirthDate is not null)
                result = FhirDateTimeParser.ParseFhirDateTime(command.BirthDate);

            if (command.Name.Use is not null) patient.NameUse = command.Name.Use;
            if (command.Name.Family is not null) patient.FamilyName = command.Name.Family;
            if (command.Name.Given is not null) patient.GivenNames = command.Name.Given;
            if (command.Gender is not null) patient.Gender = Enum.Parse<Gender>(command.Gender, ignoreCase: true);
            if (command.BirthDate is not null && result is not null)
            {
                patient.BirthDate = result.Value.parsedDate!.Value;
                patient.BirthDateOffset = result.Value.originalOffset!;
                patient.OriginalBirthDate = command.BirthDate;
            }
            if (command.Active is not null) patient.Active = command.Active.Value;

            await _context.SaveChangesAsync(cancellationToken);

            return patient.Id;
        }
    }
}
