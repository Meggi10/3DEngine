using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TAffine: TMatrix
    {
        public override TMatrix Inv()
        {
            var result = new TAffine();
            result.Assign(base.Inv());
            return result;
        }
        public TAffine(): base(4,4)
        {
            Assign(Identity(4));
        }
        public static TAffine Scaling(TVector s)
        {
            var S = new TAffine();
            for (int i = 0; i < s.Size; i++)
                S[i, i] = s[i];
            return S;
        }

        public static TAffine Translation(TVector t)
        {
            var T = new TAffine();
            for (int i = 0; i < t.Size; i++)
                T[i, 3] = t[i];
            return T;
        }

        public static TAffine Shear(TVector h)
        {
            var H = new TAffine();
            H[0, 1] = h.Z;
            H[0, 2] = h.Y;
            H[1, 2] = h.X;
            return H;
        }
        public static TAffine RotationX(double alpha)
        {
            var R = new TAffine();
            alpha *= Math.PI / 180;
            var cosA = (float)Math.Cos(alpha);
            var sinA = (float)Math.Sin(alpha);
            R[1, 1] = cosA;
            R[2, 2] = cosA;
            R[2, 1] = sinA;
            R[1, 2] = -sinA;
            return R;
        }
        public static TAffine RotationY(double beta)
        {
            var R = new TAffine();
            beta *= Math.PI / 180;
            var cosA = (float)Math.Cos(beta);
            var sinA = (float)Math.Sin(beta);
            R[0, 0] = cosA;
            R[2, 2] = cosA;
            R[2, 0] = -sinA;
            R[0, 2] = sinA;
            return R;
        }
        public static TAffine RotationZ(double gamma)
        {
            var R = new TAffine();
            gamma *= Math.PI / 180;
            var cosA = (float)Math.Cos(gamma);
            var sinA = (float)Math.Sin(gamma);
            R[1, 1] = cosA;
            R[0, 0] = cosA;
            R[0, 1] = -sinA;
            R[1, 0] = sinA;
            return R;
        }

        public static TVector operator*(TAffine left, TVector right)
        {
            var aug = new TVector(right.X, right.Y, right.Z, 1);
            aug = (TMatrix)left * aug;
            return new TVector(aug.X/aug.W,  aug.Y/aug.W, aug.Z/aug.W);
        }

        public static TMatrix operator*(TAffine left, TMatrix right)
        {
            return (TMatrix)left * right;
        }

        public static TAffine operator *(TAffine left, TAffine right)
        {
            var result = new TAffine();
            result.Assign((TMatrix)left * right);
            return result;
        }
    }
}
