using System.Xml.Linq;
using Application.Abstractions.Messaging;
using Application.Patients;
using Application.Patients.GetByBirthDate;
using Domain.Patients;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Patients
{
    internal sealed class GetByBirthDate : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("patients", async (
                HttpRequest request,
                IQueryHandler<GetByBirthDateQuery, IList<PatientResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                // Manually extract dates from query not [FromQuery] TODO: fix that!
                var dates = request.Query["date"].ToList();

                var query = new GetByBirthDateQuery(dates);

                Result<IList<PatientResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags("Patients")
            //.RequireAuthorization()
            .WithName("GetPatientByBirthDate");
        }
    }
}
