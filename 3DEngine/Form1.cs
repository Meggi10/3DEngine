using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGL;

namespace _3DEngine
{
    public partial class Form1 : Form
    {
        TScene Scene = new TScene();
        TObject3D Selected;
        Point StartPos;
        TGLContext Context = new TGLContext();
        public Form1()
        {
            InitializeComponent();
            var cube = TObject3D.CreateCube();
            cube.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            cube.Parent = Scene.Root;
            tglView1.Context.Camera.Parent = Scene.Root;
            Selected = cube;
        }
        private void tglView1_MouseDown(object sender, MouseEventArgs e)
        {
            StartPos = e.Location;
        }

        private void tglView1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var rotX = (float)Math.PI * (e.Y - StartPos.Y) / tglView1.Height;
                var rotY = (float)Math.PI * (e.X - StartPos.X) / tglView1.Width;
                if (rotX == 0 && rotY == 0)
                    return;
                var axis = new Vector3(rotX, rotY, 0);
                //axis = axis / axis.Length();
                var len = axis.Length();
                //Selected.Transform = TAffine.RotationY(rotY) * TAffine.RotationX(rotX) * Selected.Transform;
                var rot = Quaternion.CreateFromAxisAngle(axis / axis.Length(), axis.Length());
                //var rot = Quaternion.CreateFromAxisAngle(axis, (float)Math.PI / 180);
                Selected.Rotation = rot * Selected.Rotation;
                StartPos = e.Location;
                tglView1.Invalidate();
            }    
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog()  == DialogResult.OK)
            {
                var cube = Scene.Root.Children[0];
                cube.Materials[0].DiffuseMap.Path = openFileDialog1.FileName;
                //cube.Materials[0].DiffuseMap.DisplayList *= -1;
                tglView1.Invalidate();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var obj = new TObject3Dfbx();
                obj.FilePath = Path.GetDirectoryName(openFileDialog1.FileName) + "\\";
                obj = (TObject3Dfbx)obj.LoadFromFile(openFileDialog1.FileName);
                Scene.Root.Children.Clear();
                obj.Parent = Scene.Root;
                Selected = obj;
                obj.Scale *= new Vector3(0.4f, 0.4f, 0.4f);
                tglView1.Invalidate();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
                Context.CullInit(checkBox1.Checked);
                tglView1.Invalidate();
        }
    }
}
