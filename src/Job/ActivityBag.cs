using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class ActivityBag : IEnumerable<Activity>
    {
        private static readonly ConcurrentDictionary<string, Activity> Bag = new ();

        internal static bool Add(Activity activity)
        {
            return Bag.TryAdd(activity.Id, activity);
        }

        internal static bool Remove(Activity activity)
        {
            return Bag.TryRemove(activity.Id, out var ignore);
        }

        public IEnumerator<Activity> GetEnumerator()
        {
            return Bag.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}