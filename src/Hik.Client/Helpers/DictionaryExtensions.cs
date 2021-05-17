using System.Collections.Generic;

namespace Job.Extentions
{
    public static class DictionaryExtensions
    {
        public static void SafeAdd<TKey, TValue>(this Dictionary<TKey, IList<TValue>> dict,
                                                 TKey key, TValue value)
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
