using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Job
{
    public class ActivityQueue
    {
        private static readonly object _monitor = new object();
        public static int CurrentlyQueuedTasks { get { return _currentlyQueued; } }

        private static readonly RunningActivities _activities = new();

        private static readonly Dictionary<string, Queue<Parameters>> Bag = new();
        private static int _currentlyQueued;

        public static void Add(Parameters parameters)
        {
            lock (_monitor)
            {
                Interlocked.Increment(ref _currentlyQueued);
                if (Bag.ContainsKey(parameters.ClassName))
                {
                    Bag[parameters.ClassName].Enqueue(parameters);
                }
                else
                {
                    Bag.Add(parameters.ClassName, new Queue<Parameters>(new[] { parameters }));
                }
            }
        }

        public static void Execute()
        {
            while(_currentlyQueued > 0)
            {
                var tasks = Get().Select(x => new Activity(x).Start()).ToArray();
                if (tasks.Any())
                {
                    Task.WaitAny(tasks);
                }
                Task.Delay(5000);
            }
        }

        private static IEnumerable<Parameters> Get()
        {
            lock (_monitor)
            {
                foreach (var jobType in Bag.Keys)
                {
                    if (!_activities.Any(x => x.Parameters.ClassName == jobType))
                    {
                        var queue = Bag[jobType];
                        if (queue.TryDequeue(out Parameters parameters))
                        {
                            Interlocked.Decrement(ref _currentlyQueued);
                            yield return parameters;
                        }
                    }
                }
            }
        }


    }
}
