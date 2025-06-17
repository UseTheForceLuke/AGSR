using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using FhirService;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Patients.GetByBirthDate
{
    internal sealed class GetByBirthDateQueryHandler
        : IQueryHandler<GetByBirthDateQuery, IList<PatientResponse>>
    {
        private readonly IApplicationDbContext _context;

        public GetByBirthDateQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IList<PatientResponse>>> Handle(
            GetByBirthDateQuery query,
            CancellationToken cancellationToken)
        {
            var expr = _context.Patients.AsQueryable()
                .AsNoTracking();

            if (query.birthDates is not null && query.birthDates.Any())
            {
                foreach (var param in query.birthDates)
                {
                    var searchParam = FhirSearchParameter.Parse(param);
                    expr = expr.ApplyDateSearch(searchParam, p => p.BirthDate);
                }
            }


            var exprRes = expr.Select(p => new PatientResponse(
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
                    p.CreatedAt));

            var results = await exprRes.ToListAsync(cancellationToken: cancellationToken);

            return results;
        }
    }
}
