using Application.Abstractions.Messaging;

namespace Application.Users.GetByEmail
{
    public sealed record GetPatientByBirthDateQuery(string Email) : IQuery<UserResponse>;
}
