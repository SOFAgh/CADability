using CADability;
using System;
using System.IO;

/* Substitutes for System.Drawing objects, which are not allowed with WebAssembly
 * 
 */
namespace CADability.WebDrawing
{
    [Flags]
    public enum FontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikeout = 8
    }

    public struct Color
    {
        private int color;
        public Color(int argb)
        {
            color = argb;
        }
        public Color(uint argb)
        {
            color = (int)argb;
        }
        public static Color FromArgb(int argb)
        {
            return new Color(argb);
        }
        public static Color FromArgb(int alpha, Color baseColor)
        {
            return new Color((alpha & 0x00FF) << 24 | (baseColor.color & 0x00FFFFFF));
        }

        public static Color FromArgb(int red, int green, int blue)
        {
            unchecked
            {
                return new Color((int)0xFF000000 | ((red & 0x00FF) << 16) | ((green & 0x00FF) << 8) | (blue & 0x00FF));
            }
        }
        public static Color FromArgb(int alpha, int red, int green, int blue)
        {
            return new Color((alpha & 0x00FF) << 24 | (red & 0x00FF) << 16 | (green & 0x00FF) << 8 | (blue & 0x00FF));
        }

        public int ToArgb()
        {
            return color;
        }
        public byte A => (byte)((color >> 24) & 0x000000FF);
        public byte R => (byte)((color & 0x00FF0000) >> 16);
        public byte G => (byte)((color & 0x0000FF00) >> 8);
        public byte B => (byte)(color & 0x000000FF);
        public static Color Black => FromArgb(0, 0, 0);

        public static Color Red => new Color(0xFFFF0000);
        public static Color Blue => new Color(0xFF0000FF);

        public static Color Yellow => new Color(0xFFFFFF00);
        public static Color LightBlue => new Color(0xFFADD8E6);

        public static Color AliceBlue => new Color(0xFFF0F8FF);

        public static Color DarkGray => new Color(0xFFA9A9A9);

        public string Name => "0x" + ((uint)color).ToString("X");

        public static Color LightGray => new Color(0xFFD3D3D3);
        public static Color White => new Color(0xFFFFFFFF);
        public static Color LightGoldenrodYellow => new Color(0xFFFAFAD2);
        public static Color LightSkyBlue => new Color(0xFF87CEFA);
        public static Color DarkBlue => new Color(0xFF00008B);
        public static Color Gray => new Color(0xFF808080);
        public static Color LightYellow => new Color(0xFFFFFFE0);
        public static Color Green => new Color(0xFF008000);
        public static Color Cyan => new Color(0xFF00FFFF);
        public static Color Magenta =>new Color(0xFFFF00FF);

        public static bool operator ==(Color left, Color right)
        {
            return left.color == right.color;
        }
        public static bool operator !=(Color left, Color right)
        {
            return left.color != right.color;
        }
        public override bool Equals(object obj)
        {
            if (obj is Color clr) return clr.color == color;
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return color.GetHashCode();
        }
        public float GetBrightness()
        {
            float r = (float)R / 255.0f;
            float g = (float)G / 255.0f;
            float b = (float)B / 255.0f;

            float max, min;

            max = r; min = r;

            if (g > max) max = g;
            if (b > max) max = b;

            if (g < min) min = g;
            if (b < min) min = b;

            return (max + min) / 2;
        }

    }

    public struct SizeF
    {

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.Empty"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class.
        /// </devdoc>
        public static readonly SizeF Empty = new SizeF();
        private float width;
        private float height;


        /**
         * Create a new SizeF object from another size object
         */
        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.SizeF"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class
        ///    from the specified existing <see cref='System.Drawing.SizeF'/>.
        /// </devdoc>
        public SizeF(SizeF size)
        {
            width = size.width;
            height = size.height;
        }

        /**
         * Create a new SizeF object from a point
         */
        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.SizeF1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class from
        ///       the specified <see cref='System.Drawing.PointF'/>.
        ///    </para>
        /// </devdoc>
        public SizeF(PointF pt)
        {
            width = pt.X;
            height = pt.Y;
        }

        /**
         * Create a new SizeF object of the specified dimension
         */
        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.SizeF2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SizeF'/> class from
        ///       the specified dimensions.
        ///    </para>
        /// </devdoc>
        public SizeF(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.operator+"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
        ///    </para>
        /// </devdoc>
        public static SizeF operator +(SizeF sz1, SizeF sz2)
        {
            return Add(sz1, sz2);
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.operator-"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>
        ///       .
        ///    </para>
        /// </devdoc>        
        public static SizeF operator -(SizeF sz1, SizeF sz2)
        {
            return Subtract(sz1, sz2);
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.operator=="]/*' />
        /// <devdoc>
        ///    Tests whether two <see cref='System.Drawing.SizeF'/> objects
        ///    are identical.
        /// </devdoc>
        public static bool operator ==(SizeF sz1, SizeF sz2)
        {
            return sz1.Width == sz2.Width && sz1.Height == sz2.Height;
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.operator!="]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.SizeF'/> objects are different.
        ///    </para>
        /// </devdoc>
        public static bool operator !=(SizeF sz1, SizeF sz2)
        {
            return !(sz1 == sz2);
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.operatorPointF"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Converts the specified <see cref='System.Drawing.SizeF'/> to a
        ///    <see cref='System.Drawing.PointF'/>.
        ///    </para>
        /// </devdoc>
        public static explicit operator PointF(SizeF size)
        {
            return new PointF(size.Width, size.Height);
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.IsEmpty"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.SizeF'/> has zero
        ///       width and height.
        ///    </para>
        /// </devdoc>
        public bool IsEmpty
        {
            get
            {
                return width == 0 && height == 0;
            }
        }

        /**
         * Horizontal dimension
         */
        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.Width"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Represents the horizontal component of this
        ///    <see cref='System.Drawing.SizeF'/>.
        ///    </para>
        /// </devdoc>
        public float Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        /**
         * Vertical dimension
         */
        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.Height"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Represents the vertical component of this
        ///    <see cref='System.Drawing.SizeF'/>.
        ///    </para>
        /// </devdoc>
        public float Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.SizeF'/> objects.
        ///    </para>
        /// </devdoc>
        public static SizeF Add(SizeF sz1, SizeF sz2)
        {
            return new SizeF(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.operator-"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeF'/> by another <see cref='System.Drawing.SizeF'/>
        ///       .
        ///    </para>
        /// </devdoc>        
        public static SizeF Subtract(SizeF sz1, SizeF sz2)
        {
            return new SizeF(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.Equals"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests to see whether the specified object is a
        ///    <see cref='System.Drawing.SizeF'/> 
        ///    with the same dimensions as this <see cref='System.Drawing.SizeF'/>.
        /// </para>
        /// </devdoc>
        public override bool Equals(object obj)
        {
            if (!(obj is SizeF))
                return false;

            SizeF comp = (SizeF)obj;

            return (comp.Width == this.Width) &&
            (comp.Height == this.Height) &&
            (comp.GetType().Equals(GetType()));
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.GetHashCode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.ToPointF"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public PointF ToPointF()
        {
            return (PointF)this;
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.ToSize"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Size ToSize()
        {
            return Size.Truncate(this);
        }

        /// <include file='doc\SizeF.uex' path='docs/doc[@for="SizeF.ToString"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a human-readable string that represents this
        ///    <see cref='System.Drawing.SizeF'/>.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            return "{Width=" + width.ToString() + ", Height=" + height.ToString() + "}";
        }
    }
    public struct Size
    {

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Empty"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Size'/> class.
        /// </devdoc>
        public static readonly Size Empty = new Size();

        private int width;
        private int height;

        /**
         * Create a new Size object from a point
         */
        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Size"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Size'/> class from
        ///       the specified <see cref='System.Drawing.Point'/>.
        ///    </para>
        /// </devdoc>
        public Size(Point pt)
        {
            width = pt.X;
            height = pt.Y;
        }

        /**
         * Create a new Size object of the specified dimension
         */
        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Size1"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Size'/> class from
        ///    the specified dimensions.
        /// </devdoc>
        public Size(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.operatorSizeF"]/*' />
        /// <devdoc>
        ///    Converts the specified <see cref='System.Drawing.Size'/> to a
        /// <see cref='System.Drawing.SizeF'/>.
        /// </devdoc>
        public static implicit operator SizeF(Size p)
        {
            return new SizeF(p.Width, p.Height);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.operator+"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.Size'/> objects.
        ///    </para>
        /// </devdoc>
        public static Size operator +(Size sz1, Size sz2)
        {
            return Add(sz1, sz2);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.operator-"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.Size'/> by another <see cref='System.Drawing.Size'/>
        ///       .
        ///    </para>
        /// </devdoc>
        public static Size operator -(Size sz1, Size sz2)
        {
            return Subtract(sz1, sz2);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.operator=="]/*' />
        /// <devdoc>
        ///    Tests whether two <see cref='System.Drawing.Size'/> objects
        ///    are identical.
        /// </devdoc>
        public static bool operator ==(Size sz1, Size sz2)
        {
            return sz1.Width == sz2.Width && sz1.Height == sz2.Height;
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.operator!="]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.Size'/> objects are different.
        ///    </para>
        /// </devdoc>
        public static bool operator !=(Size sz1, Size sz2)
        {
            return !(sz1 == sz2);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.operatorPoint"]/*' />
        /// <devdoc>
        ///    Converts the specified <see cref='System.Drawing.Size'/> to a
        /// <see cref='System.Drawing.Point'/>.
        /// </devdoc>
        public static explicit operator Point(Size size)
        {
            return new Point(size.Width, size.Height);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.IsEmpty"]/*' />
        /// <devdoc>
        ///    Tests whether this <see cref='System.Drawing.Size'/> has zero
        ///    width and height.
        /// </devdoc>
        public bool IsEmpty
        {
            get
            {
                return width == 0 && height == 0;
            }
        }

        /**
         * Horizontal dimension
         */
        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Width"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Represents the horizontal component of this
        ///    <see cref='System.Drawing.Size'/>.
        ///    </para>
        /// </devdoc>
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        /**
         * Vertical dimension
         */
        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Height"]/*' />
        /// <devdoc>
        ///    Represents the vertical component of this
        /// <see cref='System.Drawing.Size'/>.
        /// </devdoc>
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.Size'/> objects.
        ///    </para>
        /// </devdoc>
        public static Size Add(Size sz1, Size sz2)
        {
            return new Size(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Ceiling"]/*' />
        /// <devdoc>
        ///   Converts a SizeF to a Size by performing a ceiling operation on
        ///   all the coordinates.
        /// </devdoc>
        public static Size Ceiling(SizeF value)
        {
            return new Size((int)Math.Ceiling(value.Width), (int)Math.Ceiling(value.Height));
        }

        /// <devdoc>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.Size'/> by another <see cref='System.Drawing.Size'/> .
        ///    </para>
        /// </devdoc>
        public static Size Subtract(Size sz1, Size sz2)
        {
            return new Size(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Truncate"]/*' />
        /// <devdoc>
        ///   Converts a SizeF to a Size by performing a truncate operation on
        ///   all the coordinates.
        /// </devdoc>
        public static Size Truncate(SizeF value)
        {
            return new Size((int)value.Width, (int)value.Height);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Round"]/*' />
        /// <devdoc>
        ///   Converts a SizeF to a Size by performing a round operation on
        ///   all the coordinates.
        /// </devdoc>
        public static Size Round(SizeF value)
        {
            return new Size((int)Math.Round(value.Width), (int)Math.Round(value.Height));
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.Equals"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests to see whether the specified object is a
        ///    <see cref='System.Drawing.Size'/> 
        ///    with the same dimensions as this <see cref='System.Drawing.Size'/>.
        /// </para>
        /// </devdoc>
        public override bool Equals(object obj)
        {
            if (!(obj is Size))
                return false;

            Size comp = (Size)obj;
            // Note value types can't have derived classes, so we don't need to 
            // check the types of the objects here.  -- Microsoft, 2/21/2001
            return (comp.width == this.width) &&
                   (comp.height == this.height);
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.GetHashCode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns a hash code.
        ///    </para>
        /// </devdoc>
        public override int GetHashCode()
        {
            return width ^ height;
        }

        /// <include file='doc\Size.uex' path='docs/doc[@for="Size.ToString"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a human-readable string that represents this
        ///    <see cref='System.Drawing.Size'/>.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            return "{Width=" + width.ToString() + ", Height=" + height.ToString() + "}";
        }
    }
    public struct Rectangle
    {
        int left, right, top, bottom;
        //
        public Rectangle(int x, int y, int width, int height)
        {
            left = x;
            right = x + width;
            top = y;
            bottom = y + height;
        }
        public static readonly Rectangle Empty = new Rectangle();
        public int Width
        {
            get
            {
                return right - left;
            }
            set
            {
                right = left + value;
            }
        }
        public int Top => top;
        public int Right => right;
        public int Left => left;
        public bool IsEmpty => right <= left && bottom <= top;
        public int Height
        {
            get
            {
                return bottom - top;
            }
            set
            {
                bottom = top + value;
            }
        }
        public int Bottom => bottom;
        public Point Location => new Point(left, top);
        public Size Size
        {
            get
            {
                return new Size(Width, Height);
            }
            set
            {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        public static Rectangle FromLTRB(int left, int top, int right, int bottom)
        {
            return new Rectangle(left,
                                 top,
                                 right - left,
                                 bottom - top);
        }
        internal void Inflate(int w, int h)
        {
            left -= w;
            top -= h;
            right += w;
            bottom += h;
        }
    }
    public struct RectangleF
    {
        float left, right, top, bottom;
        //
        public RectangleF(float x, float y, float width, float height)
        {
            left = x;
            right = x + width;
            top = y;
            bottom = y + height;
        }

        public float Width
        {
            get
            {
                return right - left;
            }
            set
            {
                right = left + value;
            }
        }
        public float Top => top;
        public float Right => right;
        public float Left => left;
        public bool IsEmpty => right <= left && bottom <= top;
        public float Height
        {
            get
            {
                return bottom - top;
            }
            set
            {
                bottom = top + value;
            }
        }
        public float Bottom => bottom;
        public SizeF Size
        {
            get
            {
                return new SizeF(Width, Height);
            }
            set
            {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }
        public static implicit operator RectangleF(Rectangle r)
        {
            return new RectangleF(r.Left, r.Top, r.Width, r.Height);
        }
    }

    public struct Point
    {
        int x, y;
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public int X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }
        public int Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }
    }

    public struct PointF
    {
        float x, y;
        public PointF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public float X
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }
        public float Y
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }
    }

    public class Bitmap: Image
    {
        public Bitmap() : base() { }

        public Bitmap(MemoryStream memStream)
        {
        }

        public Bitmap(string path)
        {
        }

        public double Width { get; internal set; }
        public double Height { get; internal set; }

        internal Bitmap Clone()
        {
            throw new NotImplementedException();
        }
    }
    public class Image
    {
        public Image()
        {
            base64 = "";
        }
        public string base64 { get; internal set; }
    }
}