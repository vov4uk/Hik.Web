using System;
using System.Collections.Generic;
using System.Linq;

namespace HikConsole
{
    public static class Ext
    {
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> data, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (count <= 0) return data;

            if (data is ICollection<T> collection)
                return collection.Take(collection.Count - count);

            IEnumerable<T> Skipper()
            {
                using (var enumer = data.GetEnumerator())
                {
                    T[] queue = new T[count];
                    int index = 0;

                    while (index < count && enumer.MoveNext())
                        queue[index++] = enumer.Current;

                    index = -1;
                    while (enumer.MoveNext())
                    {
                        index = (index + 1) % count;
                        yield return queue[index];
                        queue[index] = enumer.Current;
                    }
                }
            }

            return Skipper();
        }
    }
}
