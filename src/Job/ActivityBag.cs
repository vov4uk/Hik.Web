using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Job
{
    public class ActivityBag : IEnumerable<Activity>
    {
        private static ConcurrentDictionary<Guid, Activity> bag = new ConcurrentDictionary<Guid, Activity>();

        internal bool Add(Activity activity)
        {
            return bag.TryAdd(activity.Id, activity);
        }

        internal bool Remove(Activity activity)
        {
            Activity ignore = null;
            return bag.TryRemove(activity.Id, out ignore);
        }

        public IEnumerator<Activity> GetEnumerator()
        {
            return bag.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
