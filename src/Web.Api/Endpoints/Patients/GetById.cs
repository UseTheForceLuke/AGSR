using Application.Abstractions.Messaging;
using Application.Patients;
using Application.Patients.GetByBirthDate;
using Application.Patients.GetById;
using Domain.Patients;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Patients
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("patients/{id}", async (
                Guid id,
                IQueryHandler<GetPatientByIdQuery, PatientResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetPatientByIdQuery(id);

                Result<PatientResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags("Patients")
            //.RequireAuthorization()
            .WithName("GetPatientById");
        }
    }
}
