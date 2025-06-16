using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Todos;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Todos.Delete
{
    internal sealed class DeleteTodoCommandHandler
        : ICommandHandler<DeleteTodoCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUserContext _userContext;

        public DeleteTodoCommandHandler(IApplicationDbContext context, IUserContext userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task<Result> Handle(DeleteTodoCommand command, CancellationToken cancellationToken)
        {
            TodoItem? todoItem = await _context.TodoItems
                .SingleOrDefaultAsync(t => t.Id == command.TodoItemId && t.UserId == _userContext.UserId, cancellationToken);

            if (todoItem is null)
            {
                return Result.Failure(TodoItemErrors.NotFound(command.TodoItemId));
            }

            _context.TodoItems.Remove(todoItem);

            todoItem.Raise(new TodoItemDeletedDomainEvent(todoItem.Id));

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
