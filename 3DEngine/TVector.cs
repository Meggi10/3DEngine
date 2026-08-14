using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TVector
    {
        public float[] Data;
        public int Size { get { return Data.Length; } }
        public TVector( int dimCount = 3 ) {
            Data = new float[dimCount];
        }
        public TVector(params float[] data):this (data.Length)
        {
            Array.Copy(data, Data, Size);
        }
        public void Assign ( TVector v )
        {
            Array.Copy(v.Data, Data, Size);
        }
        public float this [ int index] { 
            get { return Data[index]; }
            set { Data[index] = value; }
        }
        public virtual TVector Clone () { return new TVector(Data); }
        public static TVector operator +(TVector left, float right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] += right;
            }
            return result;
        }
        public static TVector operator -(TVector left, float right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] -= right;
            }
            return result;
        }
        public static TVector operator *(TVector left, float right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] *= right;
            }
            return result;
        }

        public static TVector operator /(TVector left, float right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] /= right;
            }
            return result;
        }

        public static TVector operator +(TVector left, TVector right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] += right[i];
            }
            return result;
        }

        public static TVector operator -(TVector left, TVector right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] -= right[i];
            }
            return result;
        }

        public static TVector operator *(TVector left, TVector right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] *= right[i];
            }
            return result;
        }

        public static TVector operator /(TVector left, TVector right)
        {
            var result = left.Clone();
            for (int i = 0; i < result.Size; i++)
            {
                result[i] /= right[i];
            }
            return result;
        }
        public float Dot(TVector right)
        {
            float result = 0;
            for (int i = 0; i < Size; i++)
            {
                result += this[i] * right[i];
            }
            return result;
        }
        public float X {  get { return Data[0]; } set { Data[0] = value; } }
        public float Y { get { return Data[1]; } set { Data[1] = value; } }
        public float Z { get { return Data[2]; } set { Data[2] = value; } }
        public float W { get { return Data[3]; } set { Data[3] = value; } }

        public TVector Cross(TVector right)
        {
           var result = Clone();

            result.X = Y * right.Z - Z * right.Y;
            result.Y = Z * right.X - X * right.Z;
            result.Z = X * right.Y - Y * right.X;
            return result;
        }

        public float Norm {
            get { return (float)Math.Sqrt(Dot(this)); }

            set { Assign(this * (value/Norm)); }
        
        }
        public static TVector Uniform(int n)
        {
            var result = new TVector(n);
            for (int i = 0; i < n; i++)
                result[i] = i;
            return result;
        }
        public TVector Resample(TVector x, TVector x_)
        {
            var y_ = new TVector(x_.Size);
            var y = this;
            x_ = x_ / x_[x_.Size - 1] * x[x.Size - 1];
            int j = 1;
            for (int i = 0; i < x_.Size; i++)
            {
                while (x[j] < x_[i]) j++;
                var ratio = (x_[i] - x[j - 1]) / (x[j] - x[j - 1]);
                y_[i] = y[j - 1] * (1 - ratio) + y[j] * ratio;
            }
            return y_;
        }
    }
}
