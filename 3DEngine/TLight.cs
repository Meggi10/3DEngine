using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace Diablo3DEngine
{
    public class TLight
    {
        public TVector Position = new TVector();
        public TVector AmbientLight = new TVector(0.2f, 0.2f, 0.2f);
        public TVector DiffuseLight = new TVector(1, 1, 1);
        public TVector SpecularLight = new TVector(1, 1, 1);
    }
}
