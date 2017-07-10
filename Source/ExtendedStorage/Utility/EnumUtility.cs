using System;
using System.Collections.Generic;

namespace ExtendedStorage {
    class EnumUtility {
        public static bool TryGetNext<T>(IEnumerator<T> e, Predicate<T> predicate, out T value)
        {
            while (true)
            {
                if (!e.MoveNext())
                {
                    value = default(T);
                    return false;
                }
                value = e.Current;
                if (predicate(value))
                    return true;
            }
        }
    }
}
