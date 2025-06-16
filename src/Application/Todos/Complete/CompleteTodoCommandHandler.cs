using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Todos;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Todos.Complete
{
    internal sealed class CompleteTodoCommandHandler
        : ICommandHandler<CompleteTodoCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUserContext _userContext;

        public CompleteTodoCommandHandler(
            IApplicationDbContext context,
            IDateTimeProvider dateTimeProvider,
            IUserContext userContext)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _userContext = userContext;
        }

        public async Task<Result> Handle(CompleteTodoCommand command, CancellationToken cancellationToken)
        {
            TodoItem? todoItem = await _context.TodoItems
                .SingleOrDefaultAsync(t => t.Id == command.TodoItemId && t.UserId == _userContext.UserId, cancellationToken);

            if (todoItem is null)
            {
                return Result.Failure(TodoItemErrors.NotFound(command.TodoItemId));
            }

            if (todoItem.IsCompleted)
            {
                return Result.Failure(TodoItemErrors.AlreadyCompleted(command.TodoItemId));
            }

            todoItem.IsCompleted = true;
            todoItem.CompletedAt = _dateTimeProvider.UtcNow;

            todoItem.Raise(new TodoItemCompletedDomainEvent(todoItem.Id));

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
