using Application.Abstractions.Messaging;
using Application.Patients.Update;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Patients
{
    internal sealed class Update : IEndpoint
    {
        public sealed class NameDto
        {
            public Guid? Id { get; set; }
            public string? Use { get; set; }
            public string? Family { get; set; }
            public List<string>? Given { get; set; }
        }

        public sealed class Request
        {
            public NameDto? Name { get; set; }
            public string? Gender { get; set; }
            public string? BirthDate { get; set; }
            public bool? Active { get; set; }
        }

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("patients/{id:guid}", async (
                Guid id,
                [FromBody] Request request,
                ICommandHandler<UpdatePatientsCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                if (request == null)
                {
                    return Results.BadRequest("Request body cannot be null.");
                }

                var command = new UpdatePatientsCommand
                {
                    Name = request.Name is null ? null : new PatientNameDto
                    {
                        Id = id,
                        Use = request?.Name?.Use,
                        Family = request?.Name?.Family,
                        Given = request?.Name?.Given
                    },
                    Gender = request?.Gender,
                    BirthDate = request?.BirthDate,
                    Active = request?.Active
                };

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .WithTags("Patients")
            //.RequireAuthorization();
            .WithName("UpdatePatient");
        }
    }
}
