using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Patients;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Patients.Delete
{
    internal sealed class DeletePatientsCommandHandler
        : ICommandHandler<DeletePatientsCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUserContext _userContext;

        public DeletePatientsCommandHandler(IApplicationDbContext context, IUserContext userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task<Result> Handle(DeletePatientsCommand command, CancellationToken cancellationToken)
        {
            Patient? patient = await _context.Patients
                .SingleOrDefaultAsync(t => t.Id == command.PatientId, cancellationToken);

            if (patient is null)
            {
                return Result.Failure(PatientErrors.NotFound(command.PatientId));
            }

            _context.Patients.Remove(patient);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
