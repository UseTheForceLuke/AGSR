using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetByEmail
{
    internal sealed class GetUserByEmailQueryHandler
        : IQueryHandler<GetPatientByBirthDateQuery, UserResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUserContext _userContext;

        public GetUserByEmailQueryHandler(IApplicationDbContext context, IUserContext userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task<Result<UserResponse>> Handle(GetPatientByBirthDateQuery query, CancellationToken cancellationToken)
        {
            UserResponse? user = await _context.Users
                .Where(u => u.Email == query.Email)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure<UserResponse>(UserErrors.NotFoundByEmail);
            }

            if (user.Id != _userContext.UserId)
            {
                return Result.Failure<UserResponse>(UserErrors.Unauthorized());
            }

            return user;
        }
    }
}
