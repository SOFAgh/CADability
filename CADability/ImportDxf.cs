using CADability.GeoObject;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CADability
{
    public class ImportDxf
    {
        /*
        0-9 String (with the introduction of extended symbol names in AutoCAD 2000, the 255-character limit has been increased to 2049 single-byte characters not including the newline at the end of the line)
        10-39 Double precision 3D point value
        40-59 Double-precision floating-point value
        60-79 16-bit integer value
        90-99 32-bit integer value
        100 String (255-character maximum; less for Unicode strings)
        102 String (255-character maximum; less for Unicode strings)
        105 String representing hexadecimal (hex) handle value
        110-119 Double precision floating-point value
        120-129 Double precision floating-point value
        130-139 Double precision floating-point value
        140-149 Double precision scalar floating-point value
        160-169 64-bit integer value
        170-179 16-bit integer value
        210-239 Double-precision floating-point value
        270-279 16-bit integer value
        280-289 16-bit integer value
        290-299 Boolean flag value
        300-309 Arbitrary text string
        310-319 String representing hex value of binary chunk
        320-329 String representing hex handle value
        330-369 String representing hex object IDs
        370-379 16-bit integer value
        380-389 16-bit integer value
        390-399 String representing hex handle value
        400-409 16-bit integer value
        410-419 String
        420-429 32-bit integer value
        430-439 String
        440-449 32-bit integer value
        450-459 Long
        460-469 Double-precision floating-point value
        470-479 String
        480-481 String representing hex handle value
        999 Comment (string)
        1000-1009 String (same limits as indicated with 0-9 code range)
        1010-1059 Double-precision floating-point value
        1060-1070 16-bit integer value
        1071 32-bit integer value

            coordinates: 10-18, 110-112, 210, 1010-1013
        */
        class Tokenizer : IDisposable
        {
            StreamReader sr;
            int lineNumber;
            int nextGroupCode = -1;
            public Tokenizer(string filename)
            {
                sr = new StreamReader(filename);
                string gc = sr.ReadLine().Trim();
                bool ok = int.TryParse(gc, out nextGroupCode); // what if not ok?
                lineNumber = 0;
            }

            public bool EndOfFile
            {
                get
                {
                    return sr.EndOfStream;
                }
            }

            public int PeekNextGroupCode()
            {
                return nextGroupCode;
            }

            public (int groupCode, object val) NextToken()
            {
                if (sr.EndOfStream) return (-1, null);
                string vl = sr.ReadLine().Trim();
                lineNumber += 2;
                int groupCode = nextGroupCode;
                if (!sr.EndOfStream)
                {
                    string gc = sr.ReadLine().Trim();
                    if (!int.TryParse(gc, out nextGroupCode))
                    {
                        nextGroupCode = -1;
                    }
                }
                else
                {
                    nextGroupCode = -1;
                }

                object val = null;
                char t = '0';
                if (groupCode >= 0 && groupCode <= 9) t = 's'; // string
                else if (groupCode >= 10 && groupCode <= 18) t = 'c'; // a coordinate, the corresponding y and z values will also be read
                else if (groupCode >= 110 && groupCode <= 112) t = 'c'; // a coordinate
                else if (groupCode == 210) t = 'c'; // a coordinate
                else if (groupCode >= 1010 && groupCode <= 1013) t = 'c'; // a coordinate
                else if (groupCode >= 10 && groupCode <= 39) t = 'd'; //  Double precision 3D point value
                else if (groupCode >= 40 && groupCode <= 59) t = 'd'; //  Double-precision floating-point value
                else if (groupCode >= 60 && groupCode <= 79) t = 'i'; //  16-bit integer value
                else if (groupCode >= 90 && groupCode <= 99) t = 'i'; //  32-bit integer value
                else if (groupCode >= 100 && groupCode <= 105) t = 's'; // String (255-character maximum; less for Unicode strings)
                else if (groupCode >= 110 && groupCode <= 119) t = 'd'; //  Double precision floating-point value
                else if (groupCode >= 120 && groupCode <= 129) t = 'd'; //  Double precision floating-point value
                else if (groupCode >= 130 && groupCode <= 139) t = 'd'; //  Double precision floating-point value
                else if (groupCode >= 140 && groupCode <= 149) t = 'd'; //  Double precision scalar floating-point value
                else if (groupCode >= 160 && groupCode <= 169) t = 'l'; //  64-bit integer value
                else if (groupCode >= 170 && groupCode <= 179) t = 'i'; //  16-bit integer value
                else if (groupCode >= 210 && groupCode <= 239) t = 'd'; //  Double-precision floating-point value
                else if (groupCode >= 270 && groupCode <= 279) t = 'i'; //  16-bit integer value
                else if (groupCode >= 280 && groupCode <= 289) t = 'i'; //  16-bit integer value
                else if (groupCode >= 290 && groupCode <= 299) t = 'i'; //  Boolean flag value
                else if (groupCode >= 300 && groupCode <= 309) t = 's'; // Arbitrary text string
                else if (groupCode >= 310 && groupCode <= 319) t = 's'; // String representing hex value of binary chunk
                else if (groupCode >= 320 && groupCode <= 329) t = 's'; // String representing hex handle value
                else if (groupCode >= 330 && groupCode <= 369) t = 's'; // String representing hex object IDs
                else if (groupCode >= 370 && groupCode <= 379) t = 'i'; //  16-bit integer value
                else if (groupCode >= 380 && groupCode <= 389) t = 'i'; //  16-bit integer value
                else if (groupCode >= 390 && groupCode <= 399) t = 's'; //  String representing hex handle value
                else if (groupCode >= 400 && groupCode <= 409) t = 'i'; //  16-bit integer value
                else if (groupCode >= 410 && groupCode <= 419) t = 's'; //  String
                else if (groupCode >= 420 && groupCode <= 429) t = 'i'; //  32-bit integer value
                else if (groupCode >= 430 && groupCode <= 439) t = 's'; //  String
                else if (groupCode >= 440 && groupCode <= 449) t = 'i'; //  32-bit integer value
                else if (groupCode >= 450 && groupCode <= 459) t = 'l'; //  Long
                else if (groupCode >= 460 && groupCode <= 469) t = 'd'; //  Double-precision floating-point value
                else if (groupCode >= 470 && groupCode <= 479) t = 's'; //  String
                else if (groupCode >= 480 && groupCode <= 481) t = 's'; //  String representing hex handle value
                else if (groupCode >= 999 && groupCode <= 1009) t = 's'; //  String (same limits as indicated with 0-9 code range)
                else if (groupCode >= 1010 && groupCode <= 1059) t = 'd'; //  Double-precision floating-point value
                else if (groupCode >= 1060 && groupCode <= 1070) t = 'i'; //  16-bit integer value
                else if (groupCode == 1071) t = 'i'; //  32-bit integer value

                switch (t)
                {
                    case 'c':
                        if (double.TryParse(vl, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double x))
                        {
                            if (PeekNextGroupCode() == groupCode + 10)
                            {
                                (int gcy, object y) = NextToken();
                                if (PeekNextGroupCode() == groupCode + 20)
                                {
                                    (int gcz, object z) = NextToken();
                                    val = new GeoPoint(x, (double)y, (double)z);
                                }
                                else
                                {
                                    val = new GeoPoint2D(x, (double)y);
                                }
                            }
                            else
                            {
                                val = x;
                            }
                        }
                        break;
                    case 's':
                        val = vl;
                        break;
                    case 'i':
                        if (int.TryParse(vl, out int i))
                        {
                            val = i;
                        }
                        break;
                    case 'l':
                        if (long.TryParse(vl, out long l))
                        {
                            val = l;
                        }
                        break;
                    case 'd':
                        if (double.TryParse(vl, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double d))
                        {
                            val = d;
                        }
                        break;
                }

                if (val != null) return (groupCode, val);

                return (-1, null);
            }
            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (sr != null) sr.Dispose();
                    }
                    disposedValue = true;
                }
            }

            // This code added to correctly implement the disposable pattern.
            void IDisposable.Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
            }
            #endregion
        }
        public static void Debug()
        {
            ImportDxf idxf = new ImportDxf();
            using (Tokenizer tk = new Tokenizer(@"C:\Zeichnungen\DxfDwg\05501003.DXF"))
            {
                Dictionary<string, object> sections = new Dictionary<string, object>();
                do
                {
                    (int groupCode, object val) = tk.NextToken();
                    if (val == null) break;
                    if (groupCode == 0 && "SECTION".CompareTo(val) == 0)
                    {

                        (string name, object objects) = idxf.ReadSection(tk);
                        if (name != null) sections.Add(name, objects);
                    }
                } while (true);
            }
        }

        private (string name, object val) ReadSection(Tokenizer tk)
        {
            (int groupCode, object val) = tk.NextToken();
            if (groupCode == 2)
            {
                string name = val as string;
                switch (name)
                {
                    case "HEADER":
                        return (name, ReadHeader(tk));
                    case "TABLES":
                        break;
                    case "BLOCKS":
                        break;
                    case "ENTITIES":
                        return (name, ReadEntities(tk));
                    case "CLASSES":
                        break;
                    case "ACDSDATA":
                        break;
                    default:
                        System.Diagnostics.Trace.WriteLine("Unknown section: " + val.ToString());
                        break;
                }
            }
            while (true)
            {
                (groupCode, val) = tk.NextToken();
                if (groupCode == 0 && "ENDSEC".CompareTo(val) == 0) break;
            }
            return (null, null);
        }

        private List<IGeoObject> ReadEntities(Tokenizer tk)
        {
            List<IGeoObject> res = new List<IGeoObject>();
            bool endsec = false;
            while (!endsec)
            {
                (int groupCode, object val) = tk.NextToken();
                if (groupCode == 0 && "ENDSEC".CompareTo(val) == 0) break;
                IGeoObject ent = ReadEntity(tk, val as string);
                if (ent != null) res.Add(ent);
            }
            return res;
        }

        private IGeoObject ReadEntity(Tokenizer tk, string type)
        {
            Dictionary<int, object> entity = ReadValues(tk);
            IGeoObject res = null;
            switch (type)
            {
                case "LINE":
                    res = Line.TwoPoints((GeoPoint)entity[10], (GeoPoint)entity[11]);
                    break;
            }
            return res;
        }

        private Dictionary<int, object> ReadValues(Tokenizer tk)
        {
            Dictionary<int, object> res = new Dictionary<int, object>();
            while (!tk.EndOfFile)
            {
                if (tk.PeekNextGroupCode() == 0)
                {   // the entity ends where the next entity begins (or ENDSEC)
                    return res;
                }
                (int groupCode, object val) = tk.NextToken();
                if (res.TryGetValue(groupCode, out object exists))
                {   // make a list, if multiple entities with the same groupcode exist inside one entity
                    List<object> list;
                    if (!(exists is List<object>))
                    {
                        list = new List<object>();
                        list.Add(exists);
                        res[groupCode] = list; // overwrite single entry by list containing the single entry
                    }
                    else list = exists as List<object>;
                    list.Add(val);
                }
                else
                {
                    res[groupCode] = val;
                }
                if (groupCode == 0) break;
            }
            return res;
        }

        private Dictionary<string, object> ReadHeader(Tokenizer tk)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            while (!tk.EndOfFile)
            {
                (int groupCode, object val) = tk.NextToken();
                if (groupCode == 0 && "ENDSEC".CompareTo(val) == 0) break;
                string name = null;
                if (groupCode == 9)
                {
                    name = val as string;
                    (groupCode, val) = tk.NextToken();
                    res[name] = val;
                }
            }
            return res;
        }

        private CoordSys ArbitraryAxis(GeoVector ax)
        {
            GeoVector dirx, diry;
            if (ax.x < 1.0 / 64 && ax.y < 1.0 / 64) dirx = GeoVector.YAxis ^ ax;
            else dirx = GeoVector.ZAxis ^ ax;
            diry = ax ^ dirx;
            return new CoordSys(GeoPoint.Origin, dirx, diry);
        }
    }
}
