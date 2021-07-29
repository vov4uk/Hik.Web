using System.Collections.Generic;

namespace Hik.Client.Helpers
{
    public static class DictionaryExtensions
    {
        public static void SafeAdd<TKey, TValue>(this Dictionary<TKey, IList<TValue>> dict, TKey key, TValue value)
        {
            if (value != null)
            {
                if (dict.ContainsKey(key))
                {
                    dict[key].Add(value);
                    return;
                }

                dict[key] = new List<TValue> { value };
            }
        }
    }
}
