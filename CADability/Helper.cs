using System;
using System.Collections.Generic;

namespace CADability
{
    class Hlp
    {
        public static void Swap<T>(ref T first, ref T second)
        {
            T tmp = first;
            first = second;
            second = tmp;
        }
        /// <summary>
        /// Get the closest object. What "closest" means is defined by the parameter <paramref name="distance"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="allObjects">the collection of objects to examine</param>
        /// <param name="distance">the distance function to be evaluated</param>
        /// <returns>the closest object or a default object (which is null for classes)</returns>
        public static T GetClosest<T>(IEnumerable<T> allObjects, Func<T, double> distance)
        {
            T res = default;
            double mindist = double.MaxValue;
            if (allObjects != null)
            {
                foreach (T item in allObjects)
                {
                    double d = Math.Abs(distance(item));
                    if (d < mindist)
                    {
                        mindist = d;
                        res = item;
                    }
                }
            }
            return res;
        }
    }
}
