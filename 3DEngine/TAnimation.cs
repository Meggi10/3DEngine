using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo3DEngine
{
    public class TKeyFrame
    {
        public TObject3D Bone = new TObject3D();
        public int FrameCount = 1;
    }

    public class TAnimation
    {
        public string Name;
        public int Index;
        public bool Enabled;
        public List<TKeyFrame> Keys = new List<TKeyFrame>();
        int KeyNo;
        int FrameCount;
        int FrameNo;
        TObject3D Control;

        public void Animate()
        {
            FrameNo++;
            if (FrameNo >= FrameCount)
            {
                FrameNo = 0;
                KeyNo++;
            }
            if (KeyNo >= Keys.Count)
                KeyNo = 0;
            TKeyFrame key = Keys[KeyNo];
            FrameCount = key.FrameCount;
            var ratio = FrameCount > 1 ? FrameNo / (float)(FrameCount - 1) : 1;
            Control.Interpolate(Index, KeyNo, ratio);
        }

        //public void Accumulate()
        //{
        //    if (Control != null)
        //    {
        //        var key = new TKeyFrame();
        //        key.Bone = Control.Copy();
        //        Keys.Add(key);
        //    }
        //}

        public void Play()
        {
            Enabled = true;
            KeyNo = 0;
            FrameNo = 0;
        }

        public void Stop()
        {
            Enabled = false;
        }

        public TAnimation(TObject3D control)
        {
            Control = control;
        }

    }

}
