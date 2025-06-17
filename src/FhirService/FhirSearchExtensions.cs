using System.Linq.Expressions;

namespace FhirService
{
    public static class FhirSearchExtensions
    {
        public static IQueryable<T> ApplyDateSearch<T>(
            this IQueryable<T> query,
            FhirSearchParameter searchParam,
            Expression<Func<T, DateTimeOffset>> dateSelector)
        {
            if (!searchParam.Value.HasValue)
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            var dateProperty = Expression.Invoke(dateSelector, parameter);

            var (start, end) = searchParam.GetSearchBounds();

#pragma warning disable CA1308 // Normalize strings to uppercase
            Expression comparison = searchParam.Prefix.ToLowerInvariant() switch
            {
                "eq" => Expression.AndAlso(
                    Expression.GreaterThanOrEqual(dateProperty, Expression.Constant(start)),
                    Expression.LessThanOrEqual(dateProperty, Expression.Constant(end))
                ),
                "ne" => Expression.OrElse(
                    Expression.LessThan(dateProperty, Expression.Constant(start)),
                    Expression.GreaterThan(dateProperty, Expression.Constant(end))
                ),
                "gt" or "sa" => Expression.GreaterThan(dateProperty, Expression.Constant(start)),
                "lt" or "eb" => Expression.LessThan(dateProperty, Expression.Constant(end)),
                "ge" => Expression.GreaterThanOrEqual(dateProperty, Expression.Constant(start)),
                "le" => Expression.LessThanOrEqual(dateProperty, Expression.Constant(end)),
                _ => Expression.AndAlso(
                    Expression.GreaterThanOrEqual(dateProperty, Expression.Constant(start)),
                    Expression.LessThanOrEqual(dateProperty, Expression.Constant(end))
                )
            };
#pragma warning restore CA1308 // Normalize strings to uppercase

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            return query.Where(lambda);
        }
    }

}
