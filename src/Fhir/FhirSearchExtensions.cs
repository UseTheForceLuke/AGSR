//using Fhir;
//using System.Linq.Expressions;

//namespace FhirService
//{
//    public static class FhirSearchExtensions
//    {
//        public static IQueryable<T> ApplyBirthDateSearch<T>(
//            this IQueryable<T> query,
//            string birthDateParam,
//            Expression<Func<T, DateTime?>> birthDateSelector)
//        {
//            var searchParam = FhirSearchParameter.Parse(birthDateParam);
//            if (!searchParam.Value.HasValue)
//                return query;

//            var parameter = Expression.Parameter(typeof(T), "x");
//            var property = Expression.Invoke(birthDateSelector, parameter);
//            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(DateTime?)));

//            Expression comparison;
//            var (start, end) = searchParam.GetSearchBounds();

//            switch (searchParam.Prefix)
//            {
//                case "eq":
//                    comparison = Expression.AndAlso(
//                        Expression.GreaterThanOrEqual(property, Expression.Constant(start.DateTime)),
//                        Expression.LessThanOrEqual(property, Expression.Constant(end.DateTime)));
//                    break;
//                case "ne":
//                    comparison = Expression.OrElse(
//                        Expression.LessThan(property, Expression.Constant(start.DateTime)),
//                        Expression.GreaterThan(property, Expression.Constant(end.DateTime)));
//                    break;
//                case "gt":
//                    comparison = Expression.GreaterThan(property, Expression.Constant(end.DateTime));
//                    break;
//                case "lt":
//                    comparison = Expression.LessThan(property, Expression.Constant(start.DateTime));
//                    break;
//                case "ge":
//                    comparison = Expression.GreaterThanOrEqual(property, Expression.Constant(start.DateTime));
//                    break;
//                case "le":
//                    comparison = Expression.LessThanOrEqual(property, Expression.Constant(end.DateTime));
//                    break;
//                case "sa":
//                    comparison = Expression.GreaterThan(property, Expression.Constant(end.DateTime));
//                    break;
//                case "eb":
//                    comparison = Expression.LessThan(property, Expression.Constant(start.DateTime));
//                    break;
//                case "ap":
//                    var margin = searchParam.GetApproximateMargin();
//                    comparison = Expression.AndAlso(
//                        Expression.GreaterThanOrEqual(property, Expression.Constant(start.Add(-margin).DateTime)),
//                        Expression.LessThanOrEqual(property, Expression.Constant(end.Add(margin).DateTime)));
//                    break;
//                default:
//                    comparison = Expression.AndAlso(
//                        Expression.GreaterThanOrEqual(property, Expression.Constant(start.DateTime)),
//                        Expression.LessThanOrEqual(property, Expression.Constant(end.DateTime)));
//                    break;
//            }

//            var lambda = Expression.Lambda<Func<T, bool>>(
//                Expression.AndAlso(nullCheck, comparison), parameter);

//            return query.Where(lambda);
//        }
//    }
//}
