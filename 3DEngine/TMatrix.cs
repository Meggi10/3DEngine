using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TMatrix : TVector
    {
        public class TCols
        {  
            public TMatrix M;
            public TVector this[int i]
            {
                get { 
                    var col = new TVector(M.RowsCount);
                    Array.Copy(M.Data, i * M.RowsCount, col.Data, 0, M.RowsCount);
                    return col;
                    }

                set
                {
                    Array.Copy(value.Data, 0, M.Data, i * M.RowsCount, M.RowsCount);
                }
            }
            public void Swap(int i, int j)
            {
                var swap = this[i];
                this[i] = this[j];
                this[j] = swap;
            }
        }
        public int RowsCount;
        public int ColsCount;
        public TCols Cols = new TCols();
        public TMatrix(int rowsCount, int colsCount): base(rowsCount * colsCount)
        {
            RowsCount = rowsCount;
            ColsCount = colsCount;
            Cols.M = this;
        }

        public float this[int y, int x]
        {
            get { return Data[x * RowsCount + y]; }
            set {  Data[x * ColsCount + y] = value; }
        }

        public static TMatrix operator+(TMatrix left, float right) 
        { return (TMatrix)((TVector)left + right); }

        public static TMatrix operator -(TMatrix left, float right)
        { return (TMatrix)((TVector)left - right); }
        public static TMatrix operator *(TMatrix left, float right)
        { return (TMatrix)((TVector)left * right); }
        public static TMatrix operator /(TMatrix left, float right)
        { return (TMatrix)((TVector)left / right); }
        public static TMatrix operator +(TMatrix left, TMatrix right)
        { return (TMatrix)((TVector)left + right); }
        public static TMatrix operator -(TMatrix left, TMatrix right)
        { return (TMatrix)((TVector)left - right); }

        public static TVector operator *(TMatrix left, TVector right)
        {
            var result = new TVector(left.RowsCount);
            for (int x = 0; x < left.ColsCount; x++)
                if (right[x] != 0)
                    result += left.Cols[x] * right[x];
            return result;
        }

        public override TVector Clone()
        {
            var result = new TMatrix(RowsCount, ColsCount);
            result.Assign(this);
            return result;
        }
        public static TMatrix operator *(TMatrix left, TMatrix right)
        {
            var result = new TMatrix(left.RowsCount, right.ColsCount);
            for (int x = 0; x <  right.ColsCount; x++)
                result.Cols[x] = left * right.Cols[x];
            return result;
        }

        public TMatrix Identity(int size)
        {
            var I = new TMatrix(size, size);
            for (int i = 0; i < size; i++)
                I[i, i] = 1;
            return I;
        }

        public virtual TMatrix Inv()
        {
            var lhs = (TMatrix)Clone();
            var rhs = Identity(lhs.ColsCount);
            for (int i = 0; i < lhs.ColsCount; i++)
            {
                var pivotIdx = i;
                for (int j = i + 1; j < lhs.ColsCount; j++)
                    if (Math.Abs(lhs[i, j]) > Math.Abs(lhs[i, pivotIdx]))
                        pivotIdx = j;
                if (pivotIdx != i)
                {
                    lhs.Cols.Swap(i, pivotIdx);
                    rhs.Cols.Swap(i, pivotIdx);
                }
                var pivot = lhs[i, i];
                lhs.Cols[i] /= pivot;
                rhs.Cols[i] /= pivot;
                for (int j = 0; j < lhs.ColsCount; j++)
                {
                    if (j == i) continue;
                    var factor = lhs[i, j];
                    lhs.Cols[j] -= lhs.Cols[i] * factor;
                    rhs.Cols[j] -= rhs.Cols[i] * factor;
                }
            }
            return rhs;
        }

    }
}
