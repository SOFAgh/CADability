using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace CADability.Forms
{
    /// <summary>
    /// Images for toolbar buttons and menu icons. Images should have the size 16*15 pixel.
    /// ButtonImageList contains images from the CADability Resource and are loaded upon the 
    /// static construction of ButtonImages.
    /// To use your own Images for buttons in 
    /// CADability menu items simply add more images to the static ButtonImageList. 
    /// Some "MenuItem" entries in the MenuResource.xml have the attribute "IconNr". If IconNr
    /// is less than 10000, it is an index in the ButtonImageList (provided by CADability).
    /// If IconNr is greater or equal 10000 it is an index(-10000) to the images
    /// you have added to the ButtonImageList (starting with 0)
    /// </summary>
    public class ButtonImages
    {
        /// <summary>
        /// ImageList for buttons and menu icons, contains images from the CADability resource
        /// and may be extended by the user
        /// </summary>
        public static ImageList ButtonImageList;
        public static int OffsetUserImages; // Index des ersten UserImages
        static ButtonImages()
        {
            ButtonImageList = new ImageList();
            // this loads the fragmented button image list from the resource
            // the color of the pixel (0,0) is used to make it transparent
            ButtonImageList.ImageSize = new Size(16, 15);
            Bitmap bmp = GetBitmap("Buttons1.bmp");
            Color clr = bmp.GetPixel(0, 0);
            if (clr.A != 0) bmp.MakeTransparent(clr);
            ButtonImageList.Images.AddStrip(bmp);
            bmp = GetBitmap("Buttons2.bmp");
            clr = bmp.GetPixel(0, 0);
            if (clr.A != 0) bmp.MakeTransparent(clr);
            ButtonImageList.Images.AddStrip(bmp);
            bmp = GetBitmap("Buttons3.bmp");
            clr = bmp.GetPixel(0, 0);
            if (clr.A != 0) bmp.MakeTransparent(clr);
            ButtonImageList.Images.AddStrip(bmp);
            bmp = GetBitmap("Buttons4.bmp");
            clr = bmp.GetPixel(0, 0);
            if (clr.A != 0) bmp.MakeTransparent(clr);
            ButtonImageList.Images.AddStrip(bmp);
            bmp = GetBitmap("Buttons5.bmp");
            clr = bmp.GetPixel(0, 0);
            if (clr.A != 0) bmp.MakeTransparent(clr);
            ButtonImageList.Images.AddStrip(bmp);
            OffsetUserImages = ButtonImageList.Images.Count;
        }
        static Bitmap GetBitmap(string name)
        {
            Assembly ThisAssembly = Assembly.GetExecutingAssembly();
            System.IO.Stream str = ThisAssembly.GetManifestResourceStream("CADability.Forms.Bitmaps." + name);
            return new Bitmap(str);

        }
    }
}
