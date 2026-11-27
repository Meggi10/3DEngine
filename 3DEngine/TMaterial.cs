using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Diablo3DEngine
{
    public class TMaterial
    {
        public string Name;
        public TTexture DiffuseMap = new TTexture();
        public TTexture SpecularMap = new TTexture();
        public TTexture NormalMap = new TTexture();
        public TTexture[] Textures;
        public float Transparency {  get; set; } = 1.0f;
        public Color TransparentColor { get; set; }
        public float Shininess { get; set; }
        public float Reflectivity { get; set; }

        public static TMaterial CreateFrom(TMaterial src)
        {
            string code = JsonSerializer.Serialize(src);
            return JsonSerializer.Deserialize<TMaterial>(code);
        }
        public class TTexture
        {
            public Color Color;
            string path;
            public string Path
            {
                get => path;
                set
                {
                    path = value;
                    DisplayList *= -1;
                }
            }
            public int DisplayList;
            public bool IsOpaque = true;
            public Bitmap Texture
            {
                get
                {
                    Bitmap bmp = null;
                    if (File.Exists(Path))
                    {
                        try
                        {
                            bmp = new Bitmap(Path);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    if (bmp == null)
                    {
                        bmp = new Bitmap(1, 1);
                        bmp.SetPixel(0, 0, Color);
                    }
                    return bmp;
                }
            }
        }
        public TMaterial()
        {
            SpecularMap.Color = Color.White;
            NormalMap.Color = Color.FromArgb(127, 127, 255);
            Textures = new TTexture[] { DiffuseMap, SpecularMap, NormalMap};
        }
    }
}
