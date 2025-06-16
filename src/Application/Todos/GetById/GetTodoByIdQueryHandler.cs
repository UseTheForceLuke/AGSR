using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Todos;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Todos.GetById
{
    internal sealed class GetTodoByIdQueryHandler
        : IQueryHandler<GetTodoByIdQuery, TodoResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUserContext _userContext;

        public GetTodoByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        {
            _context = context;
            _userContext = userContext;
        }

        public async Task<Result<TodoResponse>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
        {
            TodoResponse? todo = await _context.TodoItems
                .Where(todoItem => todoItem.Id == query.TodoItemId && todoItem.UserId == _userContext.UserId)
                .Select(todoItem => new TodoResponse
                {
                    Id = todoItem.Id,
                    UserId = todoItem.UserId,
                    Description = todoItem.Description,
                    DueDate = todoItem.DueDate,
                    Labels = todoItem.Labels,
                    IsCompleted = todoItem.IsCompleted,
                    CreatedAt = todoItem.CreatedAt,
                    CompletedAt = todoItem.CompletedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (todo is null)
            {
                return Result.Failure<TodoResponse>(TodoItemErrors.NotFound(query.TodoItemId));
            }

            return todo;
        }
    }
}
