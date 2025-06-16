using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Todos.Create
{
    internal sealed class CreateTodoCommandHandler
        : ICommandHandler<CreateTodoCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUserContext _userContext;

        public CreateTodoCommandHandler(
            IApplicationDbContext context,
            IDateTimeProvider dateTimeProvider,
            IUserContext userContext)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _userContext = userContext;
        }

        public async Task<Result<Guid>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
        {
            if (_userContext.UserId != command.UserId)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            User? user = await _context.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
            }

            var todoItem = new TodoItem
            {
                UserId = user.Id,
                Description = command.Description,
                Priority = command.Priority,
                DueDate = command.DueDate,
                Labels = command.Labels,
                IsCompleted = false,
                CreatedAt = _dateTimeProvider.UtcNow
            };

            todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

            _context.TodoItems.Add(todoItem);

            await _context.SaveChangesAsync(cancellationToken);

            return todoItem.Id;
        }
    }
}
