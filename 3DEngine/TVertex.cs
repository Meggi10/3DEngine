using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace _3DEngine
{
    public class TVertex
    {
        public Vector3 Coords = new Vector3();
        //public TVector UV;
        public List<TFace> Faces = new List<TFace>();
        public List<TObject3D> Bones = new List<TObject3D>();
        public List<float> Weights = new List<float>();
        public int Index;
        Vector3 normal;
        public Vector3 Normal
        {
            get
            {
                if (normal == Vector3.Zero)
                {
                    normal = new Vector3();
                    foreach (var face in Faces)
                    {
                        normal += face.Normal;
                    }
                    if (normal == Vector3.Zero)
                        ;
                }
                return normal;
            }
            set { normal = value; }
        }
    }
}
