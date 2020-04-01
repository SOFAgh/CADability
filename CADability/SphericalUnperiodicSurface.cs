namespace CADability.GeoObject
{

    //class SphericalUnperiodicSurface : ISurfaceImpl, ISerializable, IDeserializationCallback
    //{
    //    // Eine Kugel- oder Ellipsoid-Oberfläche, deren uv-System nicht periodisch und nirgends singulär ist. Über eine ModOp wird auf die Einheitskugel abgebildet.
    //    // Der Nordpol (0,0,1) darf nicht verwendet werden, liegt außerhalb des Definitionsbereichs und aus Genauigkeitsgründen auch nichts in der Nähe des Nordpols.
    //    // Volkugeln gehen somit nicht.
    //    // Mit Mathematika ergibt sich zum Umrechnen auf die 
    //    // u0 = acos(2*u/(v*v+u*u+1))/sqrt(1-(1-2/(v*v+u*u+1))^2);
    //    // v0 = asin(1-2/(v*v+u*u+1));
    //    //
    //    // und umgekehrt (u,v) von SphericalUnperiodicSurface, (u0,v0) von SphericalSurface
    //    // u=-(cos(u0)*sin(v0))/(sin(v0)-1);
    //    // v=-(sin(u0)*sin(v0))/(sin(v0)-1);

    //    // Ebene Schnittkurven der Kugel sind 3d Kreise und in diesem uv-System ebenfalls Kreise oder Ellipsen.
    //    // Ebenso auch Schnittkurven Kugel/Kugel. Linien in den original 2d Kanten werden auch zu Kreibögen (wenn v0 konstant) oder Linien (wenn u0 konstant)

    //    // Zum konvertieren von SphericalSurface, bei gegebenem SimpleShape:
    //    // Kopiere SimpleShape um in u +2*pi und -2*pi versetzt. Suche kleinsten Abstand zwischen den Shapes. Der Mittelpunkt der Verbindungslinie ist ein Kandidat.
    //    // 

    //    internal override CndHlp3D.Surface Helper
    //    {
    //        get { throw new NotImplementedException(); }
    //    }

    //    public override ICurve FixedU(double u, double vmin, double vmax)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override ICurve FixedV(double u, double umin, double umax)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override ISurface GetModified(ModOp m)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
