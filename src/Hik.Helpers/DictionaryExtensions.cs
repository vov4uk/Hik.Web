using System.Collections.Generic;

namespace Hik.Helpers
{
    public static class DictionaryExtensions
    {
        public static void SafeAdd<TKey, TValue>(this Dictionary<TKey, IList<TValue>> dict, TKey key, TValue value)
            where TKey : notnull
        {
            if (dict != null && value != null)
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
