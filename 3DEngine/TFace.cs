using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace _3DEngine
{
    public class TFace
    {
        public List<TVertex> Vertices = new List<TVertex>();
        public List<Vector2> UV = new List<Vector2>();
        public Vector3 Tangent;
        public Vector3 Bitangent;
        Matrix3x2? Tb = null;
        public Matrix3x2 TB
        {
            get
            {
                if (Tb == null)
                {
                    //var V = new TMatrix(3, 2);
                    //V.Cols[0] = Vertices[1].Coords - Vertices[0].Coords;
                    //V.Cols[1] = Vertices[2].Coords - Vertices[0].Coords;
                    //var UW = new TMatrix(2, 2);
                    //UW.Cols[0] = UV[1] - UV[0];
                    //UW.Cols[1] = UV[2] - UV[0];
                    //Tb = V * UW.Inv();
                    var V = new Matrix3x2();
                    V.SetFirstColumn(Vertices[1].Coords - Vertices[0].Coords);
                    V.SetSecondColumn(Vertices[2].Coords - Vertices[0].Coords);
                    var UW = new Matrix3x2();
                    UW.SetFirstColumn(new Vector3(UV[1].X - UV[0].X, UV[1].Y - UV[0].Y, 0));
                    UW.SetSecondColumn(new Vector3(UV[2].X - UV[0].X, UV[2].Y - UV[0].Y, 0));
                    Matrix3x2.Invert(UW, out Matrix3x2 UWinv);
                    Tb = V * UWinv;
                    var tb = (Matrix3x2)Tb;
                    Tangent = tb.GetFirstColumn();
                    Bitangent = tb.GetSecondColumn();
                }
                return (Matrix3x2)Tb;
            }
        }
        public TMaterial Material;
        public bool IsFlat;
        Vector3 normal;
        public Vector3 Normal
        {
            get
            {
                if (normal == Vector3.Zero)
                {
                    var v10 = Vertices[0].Coords - Vertices[1].Coords;
                    var v12 = Vertices[2].Coords - Vertices[1].Coords;
                    normal = Vector3.Cross(v10, v12);
                }
                return normal;
            }
        }
        public void AddVertex(TVertex v)
        {
            Vertices.Add(v);
            v.Faces.Add(this);
        }
    }
}
