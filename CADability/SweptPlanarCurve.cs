using CADability.Curve2D;
using CADability.GeoObject;
using System;
using System.Runtime.Serialization;

namespace CADability
{


    public abstract class SweptPlanarCurve : ISurfaceImpl
    {
        ICurve2D basisCurve; // 2D Kurve

        protected SweptPlanarCurve(ICurve2D basisCurve)
        {
            this.basisCurve = basisCurve;
        }
        // die folgenden müssen Für eine konkrete Fläche überschrieben werden
        // Position muss beim Startwert die Kurve aus der xy Ebene in die Startposition bringen 
        protected abstract ModOp Position(double p);
        protected abstract double MinPar { get; }
        protected abstract double MaxPar { get; }

        #region ISurface
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.PointAt (GeoPoint2D)"/>
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override GeoPoint PointAt(GeoPoint2D uv)
        {
            return Position(uv.y) * basisCurve.PointAt(uv.x); // ModOp*GeoPoint2D geht
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.UDirection (GeoPoint2D)"/>
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override GeoVector UDirection(GeoPoint2D uv)
        {
            return Position(uv.y) * basisCurve.DirectionAt(uv.x);
        }
        /// <summary>
        /// Overrides <see cref="CADability.GeoObject.ISurfaceImpl.VDirection (GeoPoint2D)"/>
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public override GeoVector VDirection(GeoPoint2D uv)
        {
            return Position(uv.y) * GeoVector.ZAxis;
        }
        // Position of ist nicht so einfach, mit CubeHull den passenden Würfel suchen und 
        // in dessen Nähe iterieren...
        #endregion
        #region CndHlp3D Helper
        #endregion
        #region ISerializable Members
        protected SweptPlanarCurve(SerializationInfo info, StreamingContext context)
        {
        }
        /// <summary>
        /// Implements <see cref="ISerializable.GetObjectData"/>
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (<see cref="System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        protected void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        #endregion
    }
}
