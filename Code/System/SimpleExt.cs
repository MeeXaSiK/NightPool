using System;
using System.Collections.Generic;

namespace NTC.Global.System
{
    public static class SimpleExt
    {
        public static bool IsNull<T>(this List<T> list) => list.Count == 0;
        public static bool IsNull<T>(this HashSet<T> hashSet) => hashSet.Count == 0;
        public static bool IsNull<T>(this T[] array) => array.Length == 0;

        public static bool IsEqual(this string s1, string s2) => s1 == s2;
        public static bool IsEqual(this float f1, float f2) => f1 == f2;
        public static bool IsEqual(this int i1, int i2) => i1 == i2;

        public static void SetNull(this Action action) => action = null;
        public static void SetNull<T>(this Action<T> action) => action = null;
    }
}