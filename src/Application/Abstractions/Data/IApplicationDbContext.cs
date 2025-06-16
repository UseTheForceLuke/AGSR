using Domain.Todos;
using Domain.Users;
using Domain.Patients;

using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<TodoItem> TodoItems { get; }
        DbSet<Patient> Patients { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
