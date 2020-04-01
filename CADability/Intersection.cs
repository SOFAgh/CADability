namespace CADability.GeoObject
{
    /// <summary>
    /// This class provides some static methods concerning the intersection of 3D
    /// <see cref="IGeoObject"/> objects. The class <see cref="Curves"/> also provides
    /// some inetersction methods of <see cref="ICurve"/> objects.
    /// </summary>

    public class Intersection
    {
        /// <summary>
        /// Calculates intersection objects. The objects in the list toIntersect are 
        /// intersected with the plane intersectWith.
        /// </summary>
        /// <param name="toIntersect">list of objects to intersect</param>
        /// <param name="intersectWith">plane to intersect with</param>
        /// <returns>intersection objects</returns>
        public static GeoObjectList Intersect(GeoObjectList toIntersect, Plane intersectWith)
        {
            // vorläufige Implementierung, muss mit der Opencascade Umstellung geändert werden
            GeoObjectList res = new GeoObjectList();
            for (int i = 0; i < toIntersect.Count; ++i)
            {
            }
            return res;
        }
    }
}
