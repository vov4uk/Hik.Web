using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Job
{
    public class ActivityBag : IEnumerable<Activity>
    {
        private static readonly ConcurrentDictionary<Guid, Activity> Bag = new ConcurrentDictionary<Guid, Activity>();

        internal bool Add(Activity activity)
        {
            return Bag.TryAdd(activity.Id, activity);
        }

        internal bool Remove(Activity activity)
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