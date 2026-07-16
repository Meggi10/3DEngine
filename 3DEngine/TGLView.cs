using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGL;

namespace _3DEngine
{
    public partial class TGLView : UserControl
    {
        public TGLContext Context = new TGLContext();
        public TGLView()
        {
            InitializeComponent();
            Context.View = this;
            SetStyle(ControlStyles.Opaque, true);
            ResizeRedraw = true;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= (int)Win32.CS_OWNDC;
                return cp;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Context.DrawView();
        }
    }
}
