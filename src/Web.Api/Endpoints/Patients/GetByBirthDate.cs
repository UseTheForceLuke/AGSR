using System.Xml.Linq;
using Application.Abstractions.Messaging;
using Application.Patients;
using Application.Patients.GetByBirthDate;
using Domain.Patients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SharedKernel;
using Swashbuckle.AspNetCore.SwaggerGen;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Patients
{
    internal sealed class GetByBirthDate : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("patients", async (
                [FromQuery] string? date,
                IQueryHandler<GetByBirthDateQuery, IList<PatientResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var dates = date?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 ?? Array.Empty<string>();

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
