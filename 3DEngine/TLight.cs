using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TLight
    {
        public TVector AmbientLight = new TVector();
        public TVector DiffuseLight = new TVector(1, 1, 1);
        public TVector SpecularLight = new TVector();
    }
}
