using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Patients;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Patients.GetById
{
    internal sealed class GetPatientByIdQueryHandler
        : IQueryHandler<GetPatientByIdQuery, PatientResponse>
    {
        private readonly IApplicationDbContext _context;

        public GetPatientByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<PatientResponse>> Handle(
            GetPatientByIdQuery query,
            CancellationToken cancellationToken)
        {
            PatientResponse? patient = await _context.Patients
                .AsNoTracking()
                .Where(p => p.Id == query.PatientId)
                .Select(p => new PatientResponse(
                    new PatientNameResponse(
                        p.Id,
                        p.NameUse,
                        p.FamilyName,
                        p.GivenNames),
                    p.Gender.ToString(),
                    p.BirthDate,
                    p.BirthDateOffset,
                    p.OriginalBirthDate,
                    p.Active,
                    p.CreatedAt))
                .FirstOrDefaultAsync(cancellationToken);

            if (patient is null)
            {
                return Result.Failure<PatientResponse>(PatientErrors.NotFound(query.PatientId));
            }

            return patient;
        }
    }
}
