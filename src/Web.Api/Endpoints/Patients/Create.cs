using Application.Abstractions.Messaging;
using Application.Patients.Create;
using Domain.Patients;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Patients
{
    internal sealed class Create : IEndpoint
    {
        public sealed class NameDto
        {
            public string? Use { get; set; }
            public string Family { get; set; }
            public List<string>? Given { get; set; }
        }

        public sealed class Request
        {
            public NameDto Name { get; set; }
            public string Gender { get; set; }
            public string BirthDate { get; set; }
            public bool Active { get; set; } = true;
        }

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("patients", async (
                Request request,
                ICommandHandler<CreatePatientCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreatePatientCommand
                {
                    Name = new PatientNameDto
                    {
                        Id = Guid.NewGuid(),
                        Use = request.Name.Use,
                        Family = request.Name.Family,
                        Given = request.Name.Given
                    },
                    Gender = request.Gender,
                    BirthDate = request.BirthDate,
                    Active = request.Active
                };

                Result<Guid> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags("Patients")
            //.RequireAuthorization();
            .WithName("CreatePatient");
        }
    }
}
