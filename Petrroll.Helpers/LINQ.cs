using System;
using System.Collections.Generic;

namespace Petrroll.Helpers
{
    public static class LINQHelpers
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }
    }
}
