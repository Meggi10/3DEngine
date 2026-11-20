using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace Diablo3DEngine
{
    public class TLight : TObject3D
    {
        //public TVector Position = new TVector();
        public TVector AmbientLight = new TVector(0.2f, 0.2f, 0.2f);
        public TVector DiffuseLight = new TVector(1, 1, 1);
        public TVector SpecularLight = new TVector(0.05f, 0.05f, 0.05f);
        public TVector AttCoeff = new TVector(0, 0.14f, 0.07f);
        public bool IsEnabled = true;
        public bool IsDirectional;

        public override TObject3D Parent
        {
            set
            {
                Scene?.Lights.Remove(this);
                base.Parent = value;
                Scene?.Lights.Add(this);
            }
        }
    }
}
