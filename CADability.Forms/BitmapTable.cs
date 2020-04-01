using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CADability.Forms
{
    public class BitmapTable
    {
        private static Dictionary<string, Bitmap> bitmapCache;
        static BitmapTable()
        {
            bitmapCache = new Dictionary<string, Bitmap>();
        }
        public static Bitmap GetBitmap(string Name)
        {
            if (!bitmapCache.TryGetValue(Name, out Bitmap res))
            {
                Assembly ThisAssembly = Assembly.GetExecutingAssembly();
                System.IO.Stream str = ThisAssembly.GetManifestResourceStream("CADability.Forms.Bitmaps." + Name);
                // damit z.B. ein Bitmap in die resourcen kommt, einfach das Bitmap zum
                // Projekt hinzufügen und bei den Eigenschaften Build Action "Embedded resource" einstellen.
                // *.png Dateien haben den Vorteil, dass sie durchsichtige Bereiche haben können.
                res = new Bitmap(str);
                if (res != null) bitmapCache.Add(Name, res);
            }
            return res;
        }
        public static ImageList GetImageList(string Name, int Width, int Height)
        {
            if (!bitmapCache.TryGetValue(Name, out Bitmap bmp))
            {
                Assembly ThisAssembly = Assembly.GetExecutingAssembly();
                System.IO.Stream str = ThisAssembly.GetManifestResourceStream("CADability.Forms.Bitmaps." + Name);
                bmp = new Bitmap(str);
                bitmapCache[Name] = bmp;
            }
            if (bmp != null)
            {
                ImageList res = new ImageList();
                res.ImageSize = new Size(Width, Height);
                res.Images.AddStrip(bmp);
                return res;
            }
            return null;
        }
    }
}
