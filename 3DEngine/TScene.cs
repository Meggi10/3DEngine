using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace Diablo3DEngine
{
    public class TScene
    {
        public TObject3D Root = new TObject3D();
        public List<TCamera> Cameras = new List<TCamera>();
        public List<TLight> Lights = new List<TLight>();

        public TScene()
        {
            Root.Scene = this;
        }
    }
}
