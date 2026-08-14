using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGL;

namespace Diablo3DEngine
{
    public unsafe class TGLContext
    {
        public TGLView View;
        IntPtr HDC;
        IntPtr HRC;
        public TCamera Camera = new TCamera();
        //string vertexShaderPath = "/Shader/vertex.glsl";
        //string fragmentShaderPath = "/Shader/fragment.glsl";
        public const int MAX_LIGHTS = 10;
        public const int MAX_BONES = 4;
        public bool IsInited;
        uint UboCamera;
        uint UboBones;
        uint UboLights;

        public IntPtr Handle
        {
            get
            {
                if (HRC == IntPtr.Zero)
                {
                    HDC = View.CreateGraphics().GetHdc();
                    var pfd = new Win32.PIXELFORMATDESCRIPTOR();
                    var idx = Win32.ChoosePixelFormat(HDC, &pfd);
                    Win32.SetPixelFormat(HDC, idx, &pfd);
                    HRC = Win32.wglCreateContext(HDC);
                    Win32.wglMakeCurrent(HDC, HRC);
                    var gpuProgram = OpenGL.CreateProgram();
                    var vertexShader = OpenGL.CreateShader(OpenGL.GL_VERTEX_SHADER);
                    OpenGL.CompileShader(vertexShader, ReadManifestText("Resources.vertexShader.glsl.c"));
                    OpenGL.AttachShader(gpuProgram, vertexShader);
                    var fragShader = OpenGL.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
                    OpenGL.CompileShader(fragShader, ReadManifestText("Resources.fragShader.glsl.c"));
                    OpenGL.AttachShader(gpuProgram, fragShader);
                    OpenGL.LinkProgram(gpuProgram);
                    OpenGL.UseProgram(gpuProgram);
                    uint rId;
                    OpenGL.GenBuffers(1, &rId); UboCamera = rId;
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 0, UboCamera);
                    OpenGL.GenBuffers(1, &rId); UboBones = rId;
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 1, UboBones);
                    OpenGL.GenBuffers(1, &rId); UboLights = rId;
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 2, UboLights);
                }
                return HRC;
            }
        }
        static string ReadManifestText(string dotPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string rootNamespace = assembly.GetName().Name;
            string resourceName = $"{rootNamespace}.{dotPath}";
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        internal void DrawView()
        {
            if (Handle != IntPtr.Zero)
            {
                Win32.wglMakeCurrent(HDC, HRC);
                var vp = View.ClientRectangle;
                OpenGL.Viewport(vp.Left, vp.Top, vp.Width, vp.Height);
                var bg = View.BackColor;
                OpenGL.ClearColor(bg.R / 255f, bg.G / 255f, bg.B / 255f, 1);
                OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                Init();
                DrawScene();
                Win32.SwapBuffers(HDC);
            }
        }
        void Init()
        {
            if (!IsInited)
            {
                OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
                IsInited = true;
            }
        }
        public void CullInit(bool enable)
        {
            if (enable)
                OpenGL.Enable(OpenGL.GL_CULL_FACE);
            else
                OpenGL.Disable(OpenGL.GL_CULL_FACE);
        }

        private void DrawScene()
        {
            DrawObject(Camera.Root);
        }

        //[StructLayout(LayoutKind.Sequential)]
        //struct TElement
        //{
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        //    public float[] Coords;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        //    public float[] Normal;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        //    public float[] Tangent;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        //    public float[] Bitangent;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_BONES)]
        //    public float[] Bones;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_BONES)]
        //    public float[] Weights;
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        //    public float[] UV;
        //}
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TElement
        {
            public Vector3 Coords;
            public Vector3 Normal;
            public Vector3 Tangent;
            public Vector3 Bitangent;
            public Vector4 Bones;
            public Vector4 Weights;
            public Vector2 UV;
        }

        public void DrawObject(TObject3D obj)
        {
            obj.WorldTransform = obj.Transform;
            if (obj != Camera.Root && obj.Parent != Camera.Root)
                obj.WorldTransform = obj.Transform * obj.Parent.WorldTransform;
            foreach (var map in obj.Maps)
            {
                if (map.DisplayMap == 0)
                {
                    uint VAO;
                    OpenGL.GenVertexArrays(1, &VAO);
                    map.DisplayMap = VAO;
                    OpenGL.BindVertexArray(map.DisplayMap);
                    uint VBO;
                    OpenGL.GenBuffers(1, &VBO);
                    OpenGL.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO);
                    var elType = typeof(TElement);
                    var elSize = Marshal.SizeOf(elType);
                    var bufSize = 3 * map.Faces.Count * elSize;
                    var buf = Marshal.AllocHGlobal(bufSize);
                    for (int i = 0; i < map.Faces.Count; i++)
                    {
                        var face = map.Faces[i];
                        for (int j = 0; j < face.Vertices.Count; j++)
                        {
                            var vertex = face.Vertices[j];
                            var element = new TElement();
                            element.Coords = vertex.Coords;
                            element.Normal = face.IsFlat ? face.Normal : vertex.Normal;
                            if (face.UV.Count > 0)
                            {
                                element.Tangent = face.Tangent;
                                element.Bitangent = face.Bitangent;
                                element.UV = face.UV[j];
                            }
                            element.Bones = new Vector4();
                            element.Weights = new Vector4();
                            var weights = vertex.Weights.ToArray();
                            var bones = vertex.Bones.ToArray();
                            Array.Sort(weights, bones);
                            //for (int k = bones.Length - 1; k >= 0; k--)
                            //{
                            //    var idx = bones.Length - 1 - k;
                            //    if (idx >= MAX_BONES)
                            //        break;
                            //    element.Bones[idx] = obj.Bones.IndexOf(bones[k]);
                            //    element.Weights[idx] = weights[k];
                            //}
                            Marshal.StructureToPtr(element, buf + (i * 3 + j) * elSize, false);
                        }
                    }
                    OpenGL.BufferData(OpenGL.GL_ARRAY_BUFFER, bufSize, buf.ToPointer(), OpenGL.GL_STATIC_DRAW);
                    Marshal.FreeHGlobal(buf);
                    var fields = elType.GetFields();
                    var offset = 0;
                    for (uint i = 0; i < fields.Length; i++)
                    {
                        var isLast = i == fields.Length - 1;
                        var next = isLast ? elSize : (int)Marshal.OffsetOf(elType, fields[i + 1].Name);
                        var size = (next - offset) / sizeof(float);
                        OpenGL.VertexAttribPointer(i, size, OpenGL.GL_FLOAT, 0, elSize, ((IntPtr)offset).ToPointer());
                        OpenGL.EnableVertexAttribArray(i);
                        offset = next;
                    }
                }
                OpenGL.BindVertexArray(map.DisplayMap);
                var uniformLoc = 0;
                fixed (float* ptr = &obj.WorldTransform.M11)
                {
                    OpenGL.UniformMatrix4fv(uniformLoc++, 1, 0, ptr); //do sprawdzenia
                }
                if (map.Material != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        LoadTexture(map.Material.Textures[i], (uint)i);
                        OpenGL.Uniform1i(uniformLoc++, i);
                    }
                    //OpenGL.Uniform4f(uniformLoc++, map.Material.SpecularMap.Color);
                }
                //var boneMatrix = new TMatrix(16, obj.Bones.Count);
                //for (int i = 0; i < obj.Bones.Count; i++)
                //{
                //    var bone = obj.Bones[i];
                //    boneMatrix.Cols[i] = Matrix4x4.Multiply(bone.WorldTransform, bone.BindPoseInv);
                //}
                var boneMatrix = new Matrix4x4[obj.Bones.Count];
                for (int i = 0; i < obj.Bones.Count; i++)
                {
                    var bone = obj.Bones[i];
                    boneMatrix[i] = Matrix4x4.Multiply(bone.WorldTransform, bone.BindPoseInv);
                }
                //OpenGL.BindBuffer(OpenGL.GL_UNIFORM_BUFFER, UboBones[0]);
                //OpenGL.BufferDatafv(OpenGL.GL_UNIFORM_BUFFER, boneMatrix.Data, OpenGL.GL_DYNAMIC_DRAW);
                if (boneMatrix.Length > 0)
                    fixed (float* ptr = &boneMatrix[0].M11)
                    {
                        OpenGL.UniformMatrix4fv(uniformLoc++, boneMatrix.Length, 0, ptr);
                    }
                OpenGL.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3 * map.Faces.Count);
            }
            foreach (var child in obj.Children)
                DrawObject(child);
        }
        void LoadTexture(TMaterial.TTexture texture, uint unit)
        {
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + unit);
            if (texture.DisplayList <= 0)
            {
                texture.DisplayList *= -1;
                uint to = (uint)texture.DisplayList;
                OpenGL.DeleteTextures(1, &to);
                OpenGL.GenTextures(1, &to);
                texture.DisplayList = (int)to;
                OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, to);
                var bmp = texture.Texture;
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, 4, bmp.Width, bmp.Height, 0, 
                    OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, bmpData.Scan0.ToPointer());
                OpenGL.GenerateMipmap(OpenGL.GL_TEXTURE_2D);
                bmp.UnlockBits(bmpData);
            }
            else
                OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, (uint)texture.DisplayList);
        }
    }
}
