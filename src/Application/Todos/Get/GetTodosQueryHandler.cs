using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Todos.Get
{
    internal sealed class GetTodosQueryHandler
        : IQueryHandler<GetTodosQuery, List<TodoResponse>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IUserContext _userContext;

        public GetTodosQueryHandler(IApplicationDbContext context, IUserContext userContext)
        {
            _userContext = userContext;
            _context = context;
        }

        public async Task<Result<List<TodoResponse>>> Handle(GetTodosQuery query, CancellationToken cancellationToken)
        {
            if (query.UserId != _userContext.UserId)
            {
                return Result.Failure<List<TodoResponse>>(UserErrors.Unauthorized());
            }

            List<TodoResponse> todos = await _context.TodoItems
                .Where(todoItem => todoItem.UserId == query.UserId)
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
                .ToListAsync(cancellationToken);

            return todos;
        }
    }
}
