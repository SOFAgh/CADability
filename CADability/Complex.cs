#region MathNet Numerics, Vermorel, Ruegg

// MathNet Numerics, part of MathNet
//
// Copyright (c) 2004,	Joannes Vermorel, http://www.vermorel.com
//						Christoph Ruegg, http://www.cdrnet.net
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published 
// by the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public 
// License along with this program; if not, write to the Free Software
// Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#endregion

using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace MathNet.Numerics
{
    /// <summary>Complex numbers class.</summary>
    /// <remarks>
    /// <p>The class <c>Complex</c> provides all elementary operations
    /// on complex numbers. All the operators <c>+</c>, <c>-</c>,
    /// <c>*</c>, <c>/</c>, <c>==</c>, <c>!=</c> are defined in the
    /// canonical way. Additional complex trigonometric functions such 
    /// as <see cref="Complex.Cos"/>, <see cref="Complex.Acoth"/>, ... 
    /// are also provided. Note that the <c>Complex</c> structures 
    /// has two special constant values <see cref="Complex.NaN"/> and 
    /// <see cref="Complex.Infinity"/>.</p>
    /// 
    /// <p>In order to avoid possible ambiguities resulting from a 
    /// <c>Complex(double, double)</c> constructor, the static methods 
    /// <see cref="Complex.FromRealImaginary"/> and <see cref="Complex.FromModulusArgument"/>
    /// are provided instead.</p>
    /// 
    /// <code>
    /// Complex x = Complex.FromRealImaginary(1d, 2d);
    /// Complex y = Complex.FromModulusArgument(1d, Math.Pi);
    /// Complex z = (x + y) / (x - y);
    /// </code>
    /// 
    /// <p>Since there is no canonical order amoung the complex numbers,
    /// <c>Complex</c> does not implement <c>IComparable</c> but several
    /// lexicographic <c>IComparer</c> implementations are provided, see 
    /// <see cref="Complex.RealImaginaryComparer"/>,
    /// <see cref="Complex.ModulusArgumentComparer"/> and
    /// <see cref="Complex.ArgumentModulusComparer"/>.</p>
    /// 
    /// <p>For mathematical details about complex numbers, please
    /// have a look at the <a href="http://en.wikipedia.org/wiki/Complex_number">
    /// Wikipedia</a></p>
    /// </remarks>
    ///
    [Serializable]
    public struct Complex
    {
        #region Complex comparers

        private sealed class RealImaginaryLexComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class ModulusArgumentLexComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class ArgumentModulusLexComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                throw new NotImplementedException();
            }
        }

        private static IComparer realImaginaryComparer;
        private static IComparer modulusArgumentComparer;
        private static IComparer argumentModulusComparer;

        /// <summary>
        /// Gets the lexicographical comparer based on <c>(real, imaginary)</c>. 
        /// </summary>
        public static IComparer RealImaginaryComparer
        {
            get
            {
                if (realImaginaryComparer == null)
                    realImaginaryComparer = new RealImaginaryLexComparer();
                return realImaginaryComparer;
            }
        }

        /// <summary>
        /// Gets the lexicographical comparer based on <c>(modulus, argument)</c>.
        /// </summary>
        public static IComparer ModulusArgumentComparer
        {
            get
            {
                if (modulusArgumentComparer == null)
                    modulusArgumentComparer = new ModulusArgumentLexComparer();
                return modulusArgumentComparer;
            }
        }

        /// <summary>
        /// Gets the lexicographical comparer based on <c>(argument, modulus)</c>.
        /// </summary>
        public static IComparer ArgumentModulusComparer
        {
            get
            {
                if (argumentModulusComparer == null)
                    argumentModulusComparer = new ArgumentModulusLexComparer();
                return argumentModulusComparer;
            }
        }

        #endregion

        private double real;
        private double imag;

        private Complex(double real, double imag)
        {
            this.real = real;
            this.imag = imag;
        }

        #region Normalization
        // TODO: method NormalizeToUnityOrNull is never called.
        private void NormalizeToUnityOrNull()
        {
            if (double.IsPositiveInfinity(real) && double.IsPositiveInfinity(imag))
            {
                real = halfOfRoot2; imag = halfOfRoot2;
            }
            else if (double.IsPositiveInfinity(real) && double.IsNegativeInfinity(imag))
            {
                real = halfOfRoot2; imag = -halfOfRoot2;
            }
            else if (double.IsNegativeInfinity(real) && double.IsPositiveInfinity(imag))
            {
                real = -halfOfRoot2; imag = -halfOfRoot2;
            }
            else if (double.IsNegativeInfinity(real) && double.IsNegativeInfinity(imag))
            {
                real = -halfOfRoot2; imag = halfOfRoot2;
            }
            else
            {
                //Don't replace this with "Modulus"!
                double mod = Math.Sqrt(real * real + imag * imag);
                if (mod == 0)
                {
                    real = 0;
                    imag = 0;
                }
                else
                {
                    real = real / mod;
                    imag = imag / mod;
                }
            }
        }
        #endregion

        #region Constructors and Constants

        /// <summary>Constructs a <c>Complex</c> from its real
        /// and imaginary parts.</summary>
        public static Complex FromRealImaginary(double real, double imag)
        {
            return new Complex(real, imag);
        }

        /// <summary>Constructs a <c>Complex</c> from its modulus and
        /// argument.</summary>
        /// <param name="modulus">Must be non-negative.</param>
        /// <param name="argument">Real number.</param>
        public static Complex FromModulusArgument(double modulus, double argument)
        {
            if (modulus < 0d) throw new ArgumentOutOfRangeException("modulus", modulus,
                                  "A complex modulus must be non-negative.");

            return new Complex(modulus * Math.Cos(argument), modulus * Math.Sin(argument));
        }

        /// <summary>Represents the zero value. This field is constant.</summary>
        public static Complex Zero
        {
            get { return new Complex(0d, 0d); }
        }

        /// <summary>Indicates whether the <c>Complex</c> is zero.</summary>
        public bool IsZero
        {
            get { return real == 0 && imag == 0; }
        }

        /// <summary>Represents the <c>1</c> value. This field is constant.</summary>
        public static Complex One
        {
            get { return new Complex(1d, 0d); }
        }

        /// <summary>Represents the imaginary number. This field is constant.</summary>
        public static Complex I
        {
            get { return new Complex(0d, 1d); }
        }

        /// <summary>Represents a value that is not a number. This field is constant.</summary>
        public static Complex NaN
        {
            get { return new Complex(double.NaN, double.NaN); }
        }

        /// <summary>Indicates whether the provided <c>Complex</c> evaluates to a
        /// value that is not a number.</summary>
        public bool IsNaN
        {
            get { return double.IsNaN(real) || double.IsNaN(imag); }
        }

        /// <summary>Represents the infinity value. This field is constant.</summary>
        /// <remarks>The semantic associated to this value is a <c>Complex</c> of 
        /// infinite real and imaginary part. If you need more formal complex
        /// number handling (according to the Riemann Sphere and the extended
        /// complex plane C*, or using directed infinity) please check out the
        /// alternative MathNet.PreciseNumerics and MathNet.Symbolics packages
        /// instead.</remarks>
        public static Complex Infinity
        {
            get { return new Complex(double.PositiveInfinity, double.PositiveInfinity); }
        }

        /// <summary>Indicates the provided <c>Complex</c> evaluates to an
        /// infinite value.</summary>
        /// <remarks>True if it either evaluates to a complex infinity
        /// or to a directed infinity.</remarks>
        public bool IsInfinity
        {
            get { return double.IsInfinity(real) || double.IsInfinity(imag); }
        }

        /// <summary>Indicates the provided <c>Complex</c> is real.</summary>
        public bool IsReal
        {
            get { return imag == 0; }
        }

        /// <summary>Indicates the provided <c>Complex</c> is imaginary.</summary>
        public bool IsImaginary
        {
            get { return real == 0; }
        }

        #endregion

        #region Cartesian and Polar Components

        /// <summary>Gets or sets the real part of this <c>Complex</c>.</summary>
        /// <seealso cref="Imag"/>
        public double Real
        {
            get { return real; }
            set { real = value; }
        }

        /// <summary>Gets or sets the imaginary part of this <c>Complex</c>.</summary>
        /// <seealso cref="Real"/>
        public double Imag
        {
            get { return imag; }
            set { imag = value; }
        }

        /// <summary>Gets or sets the modulus of this <c>Complex</c>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an attemp
        /// is made to set a negative modulus.</exception>
        /// <remarks>If this <c>Complex</c> is zero when the modulus is set, the Complex is assumed to be positive real with an argument of zero.</remarks>
        /// <seealso cref="Argument"/>
        public double Modulus
        {
            get { return Math.Sqrt(real * real + imag * imag); }
            set
            {
                if (value < 0d) throw new ArgumentOutOfRangeException("value", value,
                                    "A complex modulus must be non-negative.");

                if (double.IsInfinity(value))
                {
                    real = value;
                    imag = value;
                }
                else
                {
                    if (real == 0d && imag == 0d)
                    {
                        real = value;
                        imag = 0;
                    }
                    else
                    {
                        double factor = value / Math.Sqrt(real * real + imag * imag);
                        real *= factor;
                        imag *= factor;
                    }
                }
            }
        }

        /// <summary>Gets or sets the squared modulus of this <c>Complex</c>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an attemp
        /// is made to set a negative modulus.</exception>
        /// <remarks>If this <c>Complex</c> is zero when the modulus is set, the Complex is assumed to be positive real with an argument of zero.</remarks>
        /// <seealso cref="Argument"/>
        public double ModulusSquared
        {
            get { return real * real + imag * imag; }
            set
            {
                if (value < 0d) throw new ArgumentOutOfRangeException("value", value,
                                    "A complex squared modulus must be non-negative.");

                if (double.IsInfinity(value))
                {
                    real = value;
                    imag = value;
                }
                else
                {
                    if (real == 0d && imag == 0d)
                    {
                        real = Math.Sqrt(value);
                        imag = 0;
                    }
                    else
                    {
                        double factor = value / (real * real + imag * imag);
                        real *= factor;
                        imag *= factor;
                    }
                }
            }
        }

        /// <summary>Gets or sets the argument of this <c>Complex</c>.</summary>
        /// <remarks>Argument always returns a value bigger than negative Pi and
        /// smaller or equal to Pi. If this <c>Complex</c> is zero, the Complex
        /// is assumed to be positive real with an argument of zero.</remarks>
        public double Argument
        {
            get
            {
                if (imag == 0 && real < 0)
                    return Math.PI;
                if (imag == 0 && real >= 0)
                    return 0;
                return Math.Atan2(imag, real);
            }
            set
            {
                double modulus = Modulus;
                real = Math.Cos(value) * modulus;
                imag = Math.Sin(value) * modulus;
            }
        }

        #endregion

        /// <summary>Gets or sets the conjugate of this <c>Complex</c>.</summary>
        /// <remarks>The semantic of <i>setting the conjugate</i> is such that
        /// <code>
        /// // a, b of type Complex
        /// a.Conjugate = b;
        /// </code>
        /// is equivalent to
        /// <code>
        /// // a, b of type Complex
        /// a = b.Conjugate
        /// </code>
        /// </remarks>
        public Complex Conjugate
        {
            get { return new Complex(real, -imag); }
            set { this = value.Conjugate; }
        }

        #region Equality & Hashing

        /// <summary>Indicates whether <c>obj</c> is equal to this instance.</summary>
        public override bool Equals(object obj)
        {
            return (obj is Complex) && this.Equals((Complex)obj);
        }

        /// <summary>Indicates whether <c>z</c> is equal to this instance.</summary>
        public bool Equals(Complex z)
        {
            if (IsNaN || z.IsNaN)
                return false;
            else
                return real == z.real && imag == z.imag;
        }

        /// <summary>Gets the hashcode of this <c>Complex</c>.</summary>
        public override int GetHashCode()
        {
            return real.GetHashCode() ^ imag.GetHashCode();
        }

        #endregion

        #region Operators

        /// <summary>Equality test.</summary>
        public static bool operator ==(Complex z1, Complex z2)
        {
            return z1.Equals(z2);
        }

        /// <summary>Inequality test.</summary>
        public static bool operator !=(Complex z1, Complex z2)
        {
            return !z1.Equals(z2);
        }

        /// <summary>Unary addition.</summary>
        public static Complex operator +(Complex z)
        {
            return z;
        }

        /// <summary>Unary minus.</summary>
        public static Complex operator -(Complex z)
        {
            return new Complex(-z.real, -z.imag);
        }

        /// <summary>Complex addition.</summary>
        public static Complex operator +(Complex z1, Complex z2)
        {
            return new Complex(z1.real + z2.real, z1.imag + z2.imag);
        }

        /// <summary>Complex subtraction.</summary>
        public static Complex operator -(Complex z1, Complex z2)
        {
            return new Complex(z1.real - z2.real, z1.imag - z2.imag);
        }

        /// <summary>Complex addition.</summary>
        public static Complex operator +(Complex z, double f)
        {
            return new Complex(z.real + f, z.imag);
        }

        /// <summary>Complex subtraction.</summary>
        public static Complex operator -(Complex z, double f)
        {
            return new Complex(z.real - f, z.imag);
        }

        /// <summary>Complex addition.</summary>
        public static Complex operator +(double f, Complex z)
        {
            return new Complex(z.real + f, z.imag);
        }

        /// <summary>Complex subtraction.</summary>
        public static Complex operator -(double f, Complex z)
        {
            return new Complex(f - z.real, -z.imag);
        }

        /// <summary>Complex multiplication.</summary>
        public static Complex operator *(Complex z1, Complex z2)
        {
            return new Complex(z1.real * z2.real - z1.imag * z2.imag, z1.real * z2.imag + z1.imag * z2.real);
        }

        /// <summary>Complex multiplication.</summary>
        public static Complex operator *(double f, Complex z)
        {
            return new Complex(z.real * f, z.imag * f);
        }

        /// <summary>Complex multiplication.</summary>
        public static Complex operator *(Complex z, double f)
        {
            return new Complex(z.real * f, z.imag * f);
        }

        /// <summary>Complex division.</summary>
        public static Complex operator /(Complex z1, Complex z2)
        {
            if (z2.IsZero)
                return Complex.Infinity;
            double z2mod = z2.ModulusSquared;
            return new Complex((z1.real * z2.real + z1.imag * z2.imag) / z2mod, (z1.imag * z2.real - z1.real * z2.imag) / z2mod);
        }

        /// <summary>Complex division.</summary>
        public static Complex operator /(double f, Complex z)
        {
            if (z.IsZero)
                return Complex.Infinity;
            double zmod = z.ModulusSquared;
            return new Complex(f * z.real / zmod, -f * z.imag / zmod);
        }

        /// <summary>Complex division.</summary>
        public static Complex operator /(Complex z, double f)
        {
            if (f == 0)
                return Complex.Infinity;
            return new Complex(z.real / f, z.imag / f);
        }

        /// <summary>Implicit conversion of a real double to a real <c>Complex</c>.</summary>
        public static implicit operator Complex(double f)
        {
            return new Complex(f, 0d);
        }

        #endregion


        #region Trigonometric Functions
        /// <summary>Trigonometric Sine (Sinus) of this <c>Complex</c>.</summary>
        public Complex Sin()
        {
            if (IsReal)
                return new Complex(Trig.Sin(real), 0d);
            return new Complex(Trig.Sin(real) * Trig.Cosh(imag), Trig.Cos(real) * Trig.Sinh(imag));
        }
        /// <summary>Trigonometric Cosine (Cosinus) of this <c>Complex</c>.</summary>
        public Complex Cos()
        {
            if (IsReal)
                return new Complex(Trig.Cos(real), 0d);
            return new Complex(Trig.Cos(real) * Trig.Cosh(imag), -Trig.Sin(real) * Trig.Sinh(imag));
        }
        /// <summary>Trigonometric Tangent (Tangens) of this <c>Complex</c>.</summary>
        public Complex Tan()
        {
            if (IsReal)
                return new Complex(Trig.Tan(real), 0d);
            double cosr = Trig.Cos(real);
            double sinhi = Trig.Sinh(imag);
            double denom = cosr * cosr + sinhi * sinhi;
            return new Complex(Trig.Sin(real) * cosr / denom, sinhi * Trig.Cosh(imag) / denom);
        }
        /// <summary>Trigonometric Cotangent (Cotangens) of this <c>Complex</c>.</summary>
        public Complex Cot()
        {
            if (IsReal)
                return new Complex(Trig.Cot(real), 0d);
            double sinr = Trig.Sin(real);
            double sinhi = Trig.Sinh(imag);
            double denom = sinr * sinr + sinhi * sinhi;
            return new Complex(sinr * Trig.Cos(real) / denom, -sinhi * Trig.Cosh(imag) / denom);
        }
        /// <summary>Trigonometric Secant (Sekans) of this <c>Complex</c>.</summary>
        public Complex Sec()
        {
            if (IsReal)
                return new Complex(Trig.Sec(real), 0d);
            double cosr = Trig.Cos(real);
            double sinhi = Trig.Sinh(imag);
            double denom = cosr * cosr + sinhi * sinhi;
            return new Complex(cosr * Trig.Cosh(imag) / denom, Trig.Sin(real) * sinhi / denom);
        }
        /// <summary>Trigonometric Cosecant (Cosekans) of this <c>Complex</c>.</summary>
        public Complex Csc()
        {
            if (IsReal)
                return new Complex(Trig.Csc(real), 0d);
            double sinr = Trig.Sin(real);
            double sinhi = Trig.Sinh(imag);
            double denom = sinr * sinr + sinhi * sinhi;
            return new Complex(sinr * Trig.Cosh(imag) / denom, -Trig.Cos(real) * sinhi / denom);
        }
        #endregion
        #region Trigonometric Arcus Functions
        /// <summary>Trigonometric Arcus Sine (Arkussinus) of this <c>Complex</c>.</summary>
        public Complex Asin()
        {
            return -Complex.I * ((1 - this.Square()).Sqrt() + Complex.I * this).Ln();
        }
        /// <summary>Trigonometric Arcus Cosine (Arkuscosinus) of this <c>Complex</c>.</summary>
        public Complex Acos()
        {
            return -Complex.I * (this + Complex.I * (1 - this.Square()).Sqrt()).Ln();
        }
        /// <summary>Trigonometric Arcus Tangent (Arkustangens) of this <c>Complex</c>.</summary>
        public Complex Atan()
        {
            Complex iz = new Complex(-imag, real); //I*this
            return new Complex(0, 0.5) * ((1 - iz).Ln() - (1 + iz).Ln());
        }
        /// <summary>Trigonometric Arcus Cotangent (Arkuscotangens) of this <c>Complex</c>.</summary>
        public Complex Acot()
        {
            Complex iz = new Complex(-imag, real); //I*this
            return new Complex(0, 0.5) * ((1 + iz).Ln() - (1 - iz).Ln()) + Math.PI / 2;
        }
        /// <summary>Trigonometric Arcus Secant (Arkussekans) of this <c>Complex</c>.</summary>
        public Complex Asec()
        {
            Complex inv = 1 / this;
            return -Complex.I * (inv + Complex.I * (1 - inv.Square()).Sqrt()).Ln();
        }
        /// <summary>Trigonometric Arcus Cosecant (Arkuscosekans) of this <c>Complex</c>.</summary>
        public Complex Acsc()
        {
            Complex inv = 1 / this;
            return -Complex.I * (Complex.I * inv + (1 - inv.Square()).Sqrt()).Ln();
        }
        #endregion
        #region Trigonometric Hyperbolic Functions
        /// <summary>Trigonometric Hyperbolic Sine (Sinus hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Sinh()
        {
            if (IsReal)
                return new Complex(Trig.Sinh(real), 0d);
            return new Complex(Trig.Sinh(real) * Trig.Cos(imag), Trig.Cosh(real) * Trig.Sin(imag));
        }
        /// <summary>Trigonometric Hyperbolic Cosine (Cosinus hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Cosh()
        {
            if (IsReal)
                return new Complex(Trig.Cosh(real), 0d);
            return new Complex(Trig.Cosh(real) * Trig.Cos(imag), Trig.Sinh(real) * Trig.Sin(imag));
        }
        /// <summary>Trigonometric Hyperbolic Tangent (Tangens hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Tanh()
        {
            if (IsReal)
                return new Complex(Trig.Tanh(real), 0d);
            double cosi = Trig.Cos(imag);
            double sinhr = Trig.Sinh(real);
            double denom = cosi * cosi + sinhr * sinhr;
            return new Complex(Trig.Cosh(real) * sinhr / denom, cosi * Trig.Sin(imag) / denom);
        }
        /// <summary>Trigonometric Hyperbolic Cotangent (Cotangens hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Coth()
        {
            if (IsReal)
                return new Complex(Trig.Coth(real), 0d);
            double sini = Trig.Sin(imag);
            double sinhr = Trig.Sinh(real);
            double denom = sini * sini + sinhr * sinhr;
            return new Complex(sinhr * Trig.Cosh(real) / denom, sini * Trig.Cos(imag) / denom);
        }
        /// <summary>Trigonometric Hyperbolic Secant (Secans hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Sech()
        {
            if (IsReal)
                return new Complex(Trig.Sech(real), 0d);
            Complex exp = this.Exp();
            return 2 * exp / (exp.Square() + 1);
        }
        /// <summary>Trigonometric Hyperbolic Cosecant (Cosecans hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Csch()
        {
            if (IsReal)
                return new Complex(Trig.Csch(real), 0d);
            Complex exp = this.Exp();
            return 2 * exp / (exp.Square() - 1);
        }
        #endregion
        #region Trigonometric Hyperbolic Area Functions
        /// <summary>Trigonometric Hyperbolic Area Sine (Areasinus hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Asinh()
        {
            return (this + (this.Square() + 1).Sqrt()).Ln();
        }
        /// <summary>Trigonometric Hyperbolic Area Cosine (Areacosinus hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Acosh()
        {
            return (this + (this - 1).Sqrt() * (this + 1).Sqrt()).Ln();
        }
        /// <summary>Trigonometric Hyperbolic Area Tangent (Areatangens hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Atanh()
        {
            return 0.5 * ((1 + this).Ln() - (1 - this).Ln());
        }
        /// <summary>Trigonometric Hyperbolic Area Cotangent (Areacotangens hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Acoth()
        {
            return 0.5 * ((this + 1).Ln() - (this - 1).Ln());
        }
        /// <summary>Trigonometric Hyperbolic Area Secant (Areasekans hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Asech()
        {
            Complex inv = 1 / this;
            return (inv + (inv - 1).Sqrt() * (inv + 1).Sqrt()).Ln();
        }
        /// <summary>Trigonometric Hyperbolic Area Cosecant (Areacosekans hyperbolicus) of this <c>Complex</c>.</summary>
        public Complex Acsch()
        {
            Complex inv = 1 / this;
            return (inv + (inv.Square() + 1).Sqrt()).Ln();
        }
        #endregion

        #region Exponential Functions
        private static readonly double halfOfRoot2 = 0.5 * Math.Sqrt(2d);

        /// <summary>Exponential of this <c>Complex</c> (exp(x), E^x).</summary>
        public Complex Exp()
        {
            double exp = Math.Exp(real);
            if (IsReal)
                return new Complex(exp, 0d);
            return new Complex(exp * Trig.Cos(imag), exp * Trig.Sin(imag));
        }
        /// <summary>Natural Logarithm of this <c>Complex</c> (Base E).</summary>
        public Complex Ln()
        {
            if (IsReal)
                return new Complex(Math.Log(real), 0d);
            return new Complex(0.5d * Math.Log(ModulusSquared), Argument);
        }
        /// <summary>Raise this <c>Complex</c> to the given value.</summary>
        public Complex Pow(Complex power)
        {
            return (power * Ln()).Exp();
        }
        /// <summary>Raise this <c>Complex</c> to the inverse of the given value.</summary>
        public Complex Root(Complex power)
        {
            return ((1 / power) * Ln()).Exp();
        }
        /// <summary>The Square (power 2) of this <c>Complex</c></summary>
        public Complex Square()
        {
            if (IsReal)
                return new Complex(real * real, 0d);
            return new Complex(real * real - imag * imag, 2 * real * imag);
        }
        /// <summary>The Square Root (power 1/2) of this <c>Complex</c></summary>
        public Complex Sqrt()
        {
            if (IsReal)
                return new Complex(Math.Sqrt(real), 0d);
            double mod = Modulus;
            if (imag > 0 || imag == 0 && real < 0)
                return new Complex(halfOfRoot2 * Math.Sqrt(mod + real), halfOfRoot2 * Math.Sqrt(mod - real));
            else
                return new Complex(halfOfRoot2 * Math.Sqrt(mod + real), -halfOfRoot2 * Math.Sqrt(mod - real));
        }
        #endregion


        #region ToString and Parse

        /// <summary>Parse a string into a <c>Complex</c>.</summary>
        /// <remarks>
        /// The adopted string representation for the complex numbers is 
        /// <i>UVW+I*XYZ</i> where <i>UVW</i> and <i>XYZ</i> are <c>double</c> 
        /// strings. Some alternative representations are <i>UVW+XYZi</i>,
        /// <i>UVW+iXYZ</i>, <i>UVW</i> and <i>iXYZ</i>. 
        /// Additionally the string <c>"NaN"</c> is mapped to 
        /// <c>Complex.NaN</c>, the string <c>"Infinity"</c> to 
        /// <c>Complex.ComplexInfinity</c>, <c>"PositiveInfinity"</c>
        /// to <c>Complex.DirectedInfinity(Complex.One)</c>,
        /// <c>"NegativeInfinity"</c> to <c>Complex.DirectedInfinity(-Complex.One)</c>
        /// and finally <c>"DirectedInfinity(WVW+I*XYZ)"</c> to <c>Complex.DirectedInfinity(WVW+I*XYZ)</c>.
        /// <code>
        /// Complex z = Complex.Parse("12.5+I*7");
        /// Complex nan = Complex.Parse("NaN");
        /// Complex infinity = Complex.Parse("Infinity");
        /// </code>
        /// This method is symetric to <see cref="ToString"/>.
        /// </remarks>
        public static Complex Parse(string complex)
        {
            ComplexParser parser = new ComplexParser(complex);
            return parser.Complex;
        }

        /// <summary>
        /// Converts this <c>Complex</c> into a <c>string</c>.
        /// </summary>
        /// <remarks>
        /// <p>This method is symmetric to <see cref="Parse"/>.</p>
        /// <p>The .Net framework may round-up the <c>double</c> values when
        /// converting them to string. The method <c>Complex.ToExactString</c>
        /// guarantied that no approximation will be done while converting
        /// the <see cref="Complex"/> to a <c>string</c>.</p>
        /// </remarks>
        /// <seealso cref="Double.ToExactString"/>
        public override string ToString()
        {
            if (IsInfinity) return "Infinity";

            if (IsNaN) return "NaN";

            if (imag == 0) return Double.ToExactString(real);

            if (real == 0)
            {
                if (imag == 1) return "I";
                if (imag == -1) return "-I";
                if (imag < 0) return "-I*" + Double.ToExactString(-imag);

                return "I*" + Double.ToExactString(imag);
            }
            else
            {
                if (imag == 1) return Double.ToExactString(real) + "+I";
                if (imag == -1) return Double.ToExactString(real) + "-I";
                if (imag < 0) return Double.ToExactString(real) + "-I*"
                                  + Double.ToExactString(-imag);

                return Double.ToExactString(real) + "+I*"
                    + Double.ToExactString(imag);
            }
        }

        private class ComplexParser
        {
            Complex complex;
            int cursor = 0;
            string source;

            public ComplexParser(string complex)
            {
                this.source = complex.ToLower().Trim();
                this.complex = ScanComplex();
            }

            #region Infrastructure
            private char Consume()
            {
                return source[cursor++];
            }
            private char LookAheadCharacterOrNull
            {
                get
                {
                    if (cursor < source.Length)
                        return source[cursor];
                    else
                        return '\0';
                }
            }
            private char LookAheadCharacter
            {
                get
                {
                    if (cursor < source.Length)
                        return source[cursor];
                    else
                        throw new ArgumentException("The given expression does not represent a complex number.", "complex");
                }
            }
            #endregion

            #region Scanners
            private Complex ScanComplex()
            {
                if (source.Equals("i"))
                    return Complex.I;
                if (source.Equals("nan"))
                    return Complex.NaN;
                if (source.Equals("infinity") || source.Equals("infty"))
                    return Complex.Infinity;
                ScanSkipWhitespace();
                Complex complex = ScanSignedComplexNumberPart();
                ScanSkipWhitespace();
                if (IsSign(LookAheadCharacterOrNull))
                    complex += ScanSignedComplexNumberPart();
                return complex;
            }
            private Complex ScanSignedComplexNumberPart()
            {
                bool negativeSign = false;
                if (IsSign(LookAheadCharacterOrNull))
                {
                    if (IsNegativeSign(LookAheadCharacter))
                        negativeSign = true;
                    Consume();
                    ScanSkipWhitespace();
                }
                if (negativeSign)
                    return -ScanComplexNumberPart();
                return ScanComplexNumberPart();
            }
            private Complex ScanComplexNumberPart()
            {
                bool imaginary = false;
                if (IsI(LookAheadCharacter))
                {
                    Consume();
                    ScanSkipWhitespace();
                    if (IsMult(LookAheadCharacterOrNull))
                        Consume();
                    ScanSkipWhitespace();
                    imaginary = true;
                }
                if (!IsNumber(LookAheadCharacterOrNull))
                    return new Complex(0d, 1d);
                double part = ScanNumber();
                ScanSkipWhitespace();
                if (IsMult(LookAheadCharacterOrNull))
                {
                    Consume();
                    ScanSkipWhitespace();
                }
                if (IsI(LookAheadCharacterOrNull))
                {
                    Consume();
                    ScanSkipWhitespace();
                    imaginary = true;
                }
                if (imaginary)
                    return new Complex(0d, part);
                else
                    return new Complex(part, 0d);
            }
            private double ScanNumber()
            {
                StringBuilder sb = new StringBuilder();
                if (IsSign(LookAheadCharacter))
                    sb.Append(Consume());
                ScanSkipWhitespace();
                ScanInteger(sb);
                ScanSkipWhitespace();
                if (IsDecimal(LookAheadCharacterOrNull))
                {
                    Consume();
                    sb.Append('.');
                    ScanInteger(sb);
                }
                if (IsE(LookAheadCharacterOrNull))
                {
                    Consume();
                    sb.Append('e');
                    if (IsSign(LookAheadCharacter))
                        sb.Append(Consume());
                    ScanInteger(sb);
                }
                return double.Parse(sb.ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }
            private void ScanInteger(StringBuilder sb)
            {
                sb.Append(Consume());
                while (IsNumber(LookAheadCharacterOrNull))
                    sb.Append(Consume());
            }
            private void ScanSkipWhitespace()
            {
                while (cursor < source.Length && !IsNotWhiteSpace(LookAheadCharacter))
                    Consume();
            }
            #endregion

            #region Indicators
            private bool IsNotWhiteSpace(char c)
            {
                return IsNumber(c) || IsDecimal(c) || IsE(c) || IsI(c) || IsSign(c) || IsMult(c);
            }
            private bool IsNumber(char c)
            {
                return c >= '0' && c <= '9';
            }
            private bool IsDecimal(char c)
            {
                return c == '.' || c == ',';
            }
            private bool IsE(char c)
            {
                return c == 'e';
            }
            private bool IsI(char c)
            {
                return c == 'i' || c == 'j';
            }
            private bool IsSign(char c)
            {
                return c == '+' || c == '-';
            }
            private bool IsNegativeSign(char c)
            {
                return c == '-';
            }
            private bool IsMult(char c)
            {
                return c == '*';
            }
            #endregion

            public Complex Complex
            {
                get { return complex; }
            }

            public double Real
            {
                get { return complex.real; }
            }

            public double Imaginary
            {
                get { return complex.imag; }
            }
        }

        #endregion


    }
}
