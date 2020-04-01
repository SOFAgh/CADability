using CADability.Attribute;
using CADability.Curve2D;
using CADability.GeoObject;
using CADability.Shapes;
using CADability.UserInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace CADability
{


    public class ImportCondor4Exception : ApplicationException
    {
        public ImportCondor4Exception(string msg)
            : base(msg)
        {
        }
    }
}
