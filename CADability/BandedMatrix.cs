using System;
using System.Collections.Generic;

namespace CADability.LinearAlgebra
{
    /// <summary>
    /// This Class stores rows of a band matrix. The data are save 
    /// in an array. But according to the size, upper and lower band 
    /// the first and last indexes are calculated and return if need. 
    /// </summary>
    class BandedRow
    {
        private int fCol;
        private int lCol;
        private double[] val;
        private int msize;
        public BandedRow(int lineIndex, int size, int uperb, int lowerb)
        {
            if (lowerb + 1 > size || uperb + 1 > size)
                throw new Exception("The second parameter not machts with the third and/or the fourth");

            if (lineIndex < 0 || lineIndex > size - 1)
                throw new Exception("The first parameter most between [0, size-1]");

            msize = size;
            if (uperb + lowerb >= size)
            { // We don't need to compress the matrix
                fCol = 0;
                lCol = size - 1;
            }
            else
            {
                int m = lineIndex - lowerb > 0 ? lineIndex - lowerb : 0;
                if (uperb + lowerb + 1 + m < size)
                {
                    fCol = m;
                    lCol = fCol + uperb + lowerb;
                }
                else
                {
                    lCol = size - 1;
                    fCol = lCol - (uperb + lowerb);
                }
            }
            if (uperb + lowerb >= size)
            {// We can't win space in this case
                val = new double[size];
            }
            else
            {
                val = new double[uperb + lowerb + 1];
            }
        }
        public double this[int index]
        {
            get
            {
                if (index >= fCol && index <= lCol)
                    return val[index - fCol];
                else
                    return 0.0;
            }
            set
            {
                if (index >= fCol && index <= lCol)
                    val[index - fCol] = value;
            }
        }
        public int lastIndex
        {
            get
            {
                return lCol;
            }
            set
            {
                if (lCol == msize - 1)
                    return;
                int n = val.Length + value;
                if (n > msize)
                    n = msize;
                double[] tmp = new double[n];
                Array.Copy(val, tmp, val.Length);
                //for (int i = 0; i < val.Length; i++)
                //    tmp[i] = val[i];
                lCol = n - 1;
                val = tmp;
            }
        }
        public int firstIndex
        {
            get
            {
                return fCol;
            }
        }
    }


    public class BandedMatrix
    {
        private int lb;
        private int ub;
        private int msize;
        private int start;
        private BandedRow[] bmatrix;
        private int[] piv;
        private bool LUdone = false;
        public BandedMatrix(int size, int lowerBand, int upperBand)
        {
            List<BandedRow> all = new List<BandedRow>();
            msize = size;
            lb = lowerBand;
            ub = upperBand;
            piv = new int[msize];
            for (int i = 0; i < msize; i++)
            {
                BandedRow l = new BandedRow(i, msize, ub, lb);
                all.Add(l);
                piv[i] = i;
            }
            bmatrix = all.ToArray();
        }
        private int min(int i)
        {
            if (i < msize - 1)
                return i;
            return msize - 1;
        }
        private int maxi(int iCol)
        {
            double d = Math.Abs(bmatrix[piv[iCol]][iCol]);
            int p = min(iCol + lb);
            int j = iCol;
            for (int k = iCol + 1; k <= p; k++)
                if (Math.Abs(bmatrix[piv[k]][iCol]) > d)
                {
                    d = Math.Abs(bmatrix[piv[k]][iCol]);
                    j = k;
                }
            return j;
        }

        public double this[int i, int j]
        {
            get
            {
                if ((i >= 0 && i < msize) && (j >= 0 && j < msize))
                    return bmatrix[i][j];
                else
                    throw new Exception("Index out of range");
                //throw new ApplicationException("to implement...");
            }
            set
            {
                if ((i >= 0 && i < msize) && (j >= 0 && j < msize))
                    bmatrix[i][j] = value;
                else
                    throw new Exception("Index out of range");
                //throw new ApplicationException("to implement...");
                LUdone = false;
                start = 0;
            }
        }

        private bool LUdecomp(bool withPivot)
        {
            for (int k = start; k < msize - 1; k++)
            {
                int i;
                start = k; //The Algorithm will continue at this stage
                //if it is invoked with pivot search
                if (withPivot)
                {
                    //Pivot search
                    i = maxi(k);
                    if (i != k)
                    {
                        int j = piv[k];
                        piv[k] = piv[i];
                        piv[i] = j;

                        for (int ii = k + 1; ii <= i; ii++)
                        {
                            j = bmatrix[piv[k]].lastIndex - bmatrix[piv[ii]].lastIndex;
                            if (j > 0) //Allocation of memory if needed
                                bmatrix[piv[ii]].lastIndex = j;
                        }
                    }
                }
                if (bmatrix[piv[k]][k] == 0)
                    return false;
                int q = min(k + lb);

                for (i = k + 1; i <= q; i++)
                {
                    bmatrix[piv[i]][k] = bmatrix[piv[i]][k] / bmatrix[piv[k]][k];
                    int p = bmatrix[piv[k]].lastIndex;
                    for (int j = k + 1; j <= p; j++)
                    {
                        bmatrix[piv[i]][j] = bmatrix[piv[i]][j] - bmatrix[piv[i]][k] * bmatrix[piv[k]][j];
                    }
                }
            }
            LUdone = true;
            return true;
        }

        public Matrix Solve(Matrix B)
        {
            if (B.RowCount != msize)
                throw new Exception("The length of the input parameter does not match");

            if (LUdone == false)
            {
                bool withoutPivot = LUdecomp(false);
                if (withoutPivot == false)
                {
                    bool withPivot = LUdecomp(true);
                    if (withPivot == false)
                        return null;  // throw new Exception("The Band Matrix is not invertible");
                    //throw new ApplicationException("to implement...");
                }
            }

            Matrix Sol = new Matrix(B.RowCount, B.ColumnCount);

            double[] y = new double[msize];
            double[] x = new double[msize];

            for (int k = 0; k < B.ColumnCount; k++)
            {
                //Permutation of B[i,k] (stored in x[]; Piv * B[,k] = x)
                for (int i = 0; i < msize; i++)
                    x[i] = B[piv[i], k];

                y[0] = x[0];
                for (int i = 1; i < msize; i++)
                {
                    double s = 0.0;

                    for (int j = bmatrix[piv[i]].firstIndex; j < i; j++) //for( int j = 0; ...; ...)
                    {
                        s += bmatrix[piv[i]][j] * y[j];
                    }
                    y[i] = x[i] - s;
                }

                //Backward substitution  (Solving U*x = y; we know U[][] and y[])
                Sol[msize - 1, k] = y[msize - 1] / bmatrix[piv[msize - 1]][msize - 1];
                for (int i = msize - 2; i >= 0; i--)
                {
                    double s = 0.0;
                    for (int j = i + 1; j <= bmatrix[piv[i]].lastIndex; j++) //for( ...; ...; j < msize)
                        s += bmatrix[piv[i]][j] * Sol[j, k];
                    Sol[i, k] = (y[i] - s) / bmatrix[piv[i]][i];
                }
            }
            return Sol;
        }
    }
}
