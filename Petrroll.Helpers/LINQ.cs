using System;
using System.Collections;
using System.Collections.Generic;

namespace Petrroll.Helpers
{
    public static class LINQHelpers
    {
        public static int IndexOf(this IEnumerable enumeration, object obj)
        {
            int i = 0;
            foreach(var a in enumeration)
            {
                if(a == null & obj == null) { return i; }
                if (a != null && a.Equals(obj)) { return i; }

                i++;
            }

            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> enumeration, T obj)
        {
            int i = 0;
            foreach (var a in enumeration)
            {
                #pragma warning disable RECS0017 // Possible compare of value type with 'null'
                if (a == null & obj == null) { return i; }
                if (a != null && a.Equals(obj)) { return i; }
                #pragma warning restore RECS0017 // Possible compare of value type with 'null'

                i++;
            }

            return -1;
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static void ForEach(this IEnumerable enumeration, Action<object> action)
        {
            foreach (object item in enumeration)
            {
                action(item);
            }
        }
    }
}
