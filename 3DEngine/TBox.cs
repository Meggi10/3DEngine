using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace _3DEngine
{
    public class TBox
    {
        public TVector Origin = new TVector();
        public TVector Scale = new TVector();
        public TVector LBN
        {
            get { return Origin - Scale; }
            set
            {
                Origin = (value + RTF) / 2;
                Scale = Origin - value;
            }
        }
        public TVector RTF
        {
            get { return Origin + Scale; }
            set
            {
                Origin = (LBN + value) / 2;
                Scale = value - Origin;
            }
        }
        
        public void Union(TVector v)
        {
            var lbn = LBN;
            var rtf = RTF;
            for (var i = 0; i < 3; i++)
            {
                if (v[i] < lbn[i])
                    lbn[i] = v[i];
                if (v[i] > rtf[i])
                    rtf[i] = v[i];
            }
            LBN = lbn;
            RTF = rtf;
        }
        public TAffine Transform
        {
            get
            {
                return TAffine.Translation(Origin) * TAffine.Scaling(Scale);
            }
        }
        public void Assign(TBox box)
        {
            Origin = box.Origin.Clone();
            Scale = box.Scale.Clone();
        }
    }
}
