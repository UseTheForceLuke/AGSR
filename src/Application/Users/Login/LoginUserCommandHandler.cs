using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Login
{
    internal sealed class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, string>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenProvider _tokenProvider;

        public LoginUserCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ITokenProvider tokenProvider)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenProvider = tokenProvider;
        }

        public async Task<Result<string>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User? user = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

            if (user is null)
            {
                return Result.Failure<string>(UserErrors.NotFoundByEmail);
            }

            bool verified = _passwordHasher.Verify(command.Password, user.PasswordHash);

            if (!verified)
            {
                return Result.Failure<string>(UserErrors.NotFoundByEmail);
            }

            string token = _tokenProvider.Create(user);

            return token;
        }
    }
}
