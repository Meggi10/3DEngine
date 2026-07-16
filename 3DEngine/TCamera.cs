using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace _3DEngine
{
    public class TCamera : TObject3D
    {
        public bool IsPerspective;
        public float Fovy = 90;
        protected TBox Clip = new TBox();
        protected TBox Zoom = new TBox();
        public TCamera()
        {
            Clip.Scale = new TVector(1, 1, 1);
            Zoom.Scale = new TVector(1, 1, 1);
        }
        public Matrix4x4 Projection
        {
            get
            {
                Matrix4x4.Invert(WorldTransform, out Matrix4x4 proj);
                var clip = new TBox();
                clip.Assign(Clip);
                if (IsPerspective)
                {
                    //var permute = new TAffine();
                    //permute.Cols.Swap(2, 3);
                    ////proj.Mult(permute);
                    //proj = permute * proj;
                    var aspect = Clip.Scale.Y / Clip.Scale.X;
                    //var tgHalfFovy = (float)Math.Tan(Math.PI * Fovy / 360);
                    //clip.Scale.X = tgHalfFovy / aspect;
                    //clip.Scale.Y = tgHalfFovy;
                    //clip.Scale.Z = -0.5f;
                    //clip.Origin.Z = 0.5f;
                    var fovyRad = Math.PI * Fovy / 180;
                    var persp = Matrix4x4.CreatePerspectiveFieldOfView((float)fovyRad, aspect, 1, 1000);
                    proj = Matrix4x4.Multiply(proj, persp);
                }
                else
                {
                    //proj.Mult(clip.Transform.Inv);
                    //proj = (TAffine)clip.Transform.Inv() * proj;
                    //proj = Zoom.Transform * proj;
                    proj = proj.ScaleFast(new Vector3(clip.Scale.X, clip.Scale.Y, clip.Scale.Z));
                    proj = proj.TranslateFast(new Vector3(clip.Origin.X, clip.Origin.Y, clip.Origin.Z));
                }
                return proj;
            }
        }

    }
}
