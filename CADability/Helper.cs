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
        /// <typeparam name="T">type of objects to test</typeparam>
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
        /// <summary>
        /// Get the closest object. What "closest" means is defined by the parameter <paramref name="distance"/>.
        /// Special case of <see cref="GetClosest{T}(IEnumerable{T}, Func{T, double})"/> for arrays
        /// </summary>
        /// <typeparam name="T">type of objects to test</typeparam>
        /// <param name="allObjects">the collection of objects to examine</param>
        /// <param name="distance">the distance function to be evaluated</param>
        /// <returns>the closest object or a default object (which is null for classes)</returns>
        public static T GetClosest<T>(T[] allObjects, Func<T, double> distance)
        {
            if (allObjects.Length == 1) return allObjects[0];
            return GetClosest(allObjects as IEnumerable<T>, distance);
        }
        /// <summary>
        /// Get the closest object. What "closest" means is defined by the parameter <paramref name="distance"/>.
        /// Special case of <see cref="GetClosest{T}(IEnumerable{T}, Func{T, double})"/> for List<typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">type of objects to test</typeparam>
        /// <param name="allObjects">the collection of objects to examine</param>
        /// <param name="distance">the distance function to be evaluated</param>
        /// <returns>the closest object or a default object (which is null for classes)</returns>
        public static T GetClosest<T>(List<T> allObjects, Func<T, double> distance)
        {
            if (allObjects.Count == 1) return allObjects[0];
            return GetClosest(allObjects as IEnumerable<T>, distance);
        }

    }
}
