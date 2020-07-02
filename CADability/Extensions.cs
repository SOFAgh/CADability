using System;
using System.Collections;
using System.Collections.Generic;

namespace CADability
{
    public class CombinedEnumerable<T> : IEnumerable<T>, IEnumerator<T>, IEnumerator
    {
        IEnumerable<T>[] toEnumerate;
        int currentIndex;
        IEnumerator<T> currentEnumerator;
        public CombinedEnumerable(params IEnumerable<T>[] enums)
        {
            toEnumerate = enums;
            currentIndex = 0;
            currentEnumerator = null;
        }

        T IEnumerator<T>.Current
        {
            get
            {
                return currentEnumerator.Current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return currentEnumerator.Current;
            }
        }

        void IDisposable.Dispose()
        {
            currentIndex = -1;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        bool IEnumerator.MoveNext()
        {
            if (currentEnumerator == null) currentEnumerator = toEnumerate[0].GetEnumerator();
            while (!currentEnumerator.MoveNext())
            {
                ++currentIndex;
                if (currentIndex < toEnumerate.Length)
                {
                    currentEnumerator = toEnumerate[currentIndex].GetEnumerator();
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        void IEnumerator.Reset()
        {
            currentIndex = 0;
            currentEnumerator = null;
        }
        //public static IEnumerable<T> operator +(IEnumerable<T> t1, IEnumerable<T> t2)
        //{
        //    return new CombinedEnumerable(t1, t2);
        //}
    }

    static class Extensions
    {
        public static IEnumerable<T> Combine<T>(params IEnumerable<T>[] enumerators)
        {
            return new CombinedEnumerable<T>(enumerators);
        }
    }
}
