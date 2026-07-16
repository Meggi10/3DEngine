using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace _3DEngine
{
    public static class MatrixExtensions
    {
        /// <summary>
        /// Szybko skaluje istniejącą macierz modyfikując jej wiersze (9 mnożeń).
        /// </summary>
        public static Matrix4x4 ScaleFast(ref this Matrix4x4 matrix, Vector3 scale)
        {
            matrix.M11 *= scale.X; matrix.M12 *= scale.X; matrix.M13 *= scale.X;
            matrix.M21 *= scale.Y; matrix.M22 *= scale.Y; matrix.M23 *= scale.Y;
            matrix.M31 *= scale.Z; matrix.M32 *= scale.Z; matrix.M33 *= scale.Z;
            return matrix;
        }

        /// <summary>
        /// Szybko nakłada translację bezpośrednio na czwarty wiersz macierzy.
        /// </summary>
        public static Matrix4x4 TranslateFast(ref this Matrix4x4 matrix, Vector3 translation)
        {
            matrix.M41 = translation.X;
            matrix.M42 = translation.Y;
            matrix.M43 = translation.Z;
            return matrix;
        }
        public static void SetFirstColumn(ref this Matrix3x2 matrix, Vector3 vector)
        {
            matrix.M11 = vector.X;
            matrix.M21 = vector.Y;
            matrix.M31 = vector.Z;
        }
        public static void SetSecondColumn(ref this Matrix3x2 matrix, Vector3 vector)
        {
            matrix.M12 = vector.X;
            matrix.M22 = vector.Y;
            matrix.M32 = vector.Z;
        }
        public static Vector3 GetFirstColumn(ref this Matrix3x2 matrix)
        {
            var vector = new Vector3();
            vector.X = matrix.M11;
            vector.Y = matrix.M21;
            vector.Z = matrix.M31;
            return vector;
        }
        public static Vector3 GetSecondColumn(ref this Matrix3x2 matrix)
        {
            var vector = new Vector3();
            vector.X = matrix.M12;
            vector.Y = matrix.M22;
            vector.Z = matrix.M32;
            return vector;
        }
    }


}
