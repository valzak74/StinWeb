using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models
{
    public static class LinqExtensions
    {
        public static IEnumerable<TSource> TakeWhileAggregate<TSource, TAccumulate>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, bool> predicate
        )
        {
            TAccumulate accumulator = seed;
            foreach (TSource item in source)
            {
                accumulator = func(accumulator, item);
                if (predicate(accumulator))
                {
                    yield return item;
                }
                else
                {
                    yield break;
                }
            }
        }
        public static TSource AggregateWhile<TSource>(this IEnumerable<TSource> source,
                                                 Func<TSource, TSource, TSource> func,
                                                 Func<TSource, bool> predicate)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                TSource result = e.Current;
                TSource tmp = default(TSource);
                while (e.MoveNext() && predicate(tmp = func(result, e.Current)))
                    result = tmp;
                return result;
            }
        }
        public static IEnumerable<IGrouping<TKey, TSource>> ThenBy<TSource, TKey>(
            this IEnumerable<IGrouping<TKey, TSource>> source,
            Func<TSource, TKey> keySelector)
        {

            var unGroup = source.SelectMany(sm => sm).Distinct(); // thank you flq at http://stackoverflow.com/questions/462879/convert-listlistt-into-listt-in-c-sharp
            var reGroup = unGroup.GroupBy(keySelector);

            return source.Concat(reGroup);
        }
    }
}
