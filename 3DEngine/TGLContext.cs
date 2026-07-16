using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGL;

namespace _3DEngine
{
    public class TGLContext
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
        int[] UboCamera = new int[1];
        int[] UboBones = new int[1];
        int[] UboLights = new int[1];

        public IntPtr Handle
        {
            get
            {
                if (HRC == IntPtr.Zero)
                {
                    HDC = View.CreateGraphics().GetHdc();
                    var pfd = new Win32.PIXELFORMATDESCRIPTOR();
                    var idx = Win32.ChoosePixelFormat(HDC, pfd);
                    Win32.SetPixelFormat(HDC, idx, pfd);
                    HRC = Win32.wglCreateContext(HDC);
                    Win32.wglMakeCurrent(HDC, HRC);
                    var gpuProgram = OpenGL.CreateProgram();
                    OpenGL.AttachShader(gpuProgram, CreateShader(OpenGL.GL_VERTEX_SHADER));
                    OpenGL.AttachShader(gpuProgram, CreateShader(OpenGL.GL_FRAGMENT_SHADER));
                    OpenGL.LinkProgram(gpuProgram);
                    OpenGL.UseProgram(gpuProgram);
                    OpenGL.GenBuffers(1, UboCamera);
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 0, UboCamera[0]);
                    OpenGL.GenBuffers(1, UboBones);
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 1, UboBones[0]);
                    OpenGL.GenBuffers(1, UboLights);
                    OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 2, UboLights[0]);
                }
                return HRC;
            }
        }
        int CreateShader(int shaderType, [CallerFilePath] string path = null)
        {
            var shader = OpenGL.CreateShader(shaderType);
            var source = "";
            if (shaderType == OpenGL.GL_VERTEX_SHADER)
                source = Properties.Resources.vertexShader_glsl;
            //path += vertexShaderPath;
            else if (shaderType == OpenGL.GL_FRAGMENT_SHADER)
                source = Properties.Resources.fragShader_glsl;
                //path += fragmentShaderPath;
                //var source = System.IO.File.ReadAllText(path);

                OpenGL.ShaderSource(shader, source);
            OpenGL.CompileShader(shader);
            var status = new int[1];
            OpenGL.GetShader(shader, OpenGL.GL_COMPILE_STATUS, status);
            if (status[0] == 0)
            {
                var maxLength = new int[1];
                OpenGL.GetShader(shader, OpenGL.GL_INFO_LOG_LENGTH, maxLength);
                var log = new StringBuilder(maxLength[0]);
                OpenGL.GetShaderInfoLog(shader, maxLength[0], IntPtr.Zero, log);
            }
            return shader;
        }
        internal void DrawView()
        {
            if (Handle != IntPtr.Zero)
            {
                Win32.wglMakeCurrent(HDC, HRC);
                var vp = View.ClientRectangle;
                OpenGL.glViewport(vp.Left, vp.Top, vp.Width, vp.Height);
                var bg = View.BackColor;
                OpenGL.glClearColor(bg.R / 255f, bg.G / 255f, bg.B / 255f, 1);
                OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                Init();
                DrawScene();
                Win32.SwapBuffers(HDC);
            }
        }
        void Init()
        {
            if (!IsInited)
            {
                OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
                IsInited = true;
            }
        }
        public void CullInit(bool enable)
        {
            if (enable)
                OpenGL.glEnable(OpenGL.GL_CULL_FACE);
            else
                OpenGL.glDisable(OpenGL.GL_CULL_FACE);
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
                    var VAO = new int[1];
                    OpenGL.GenVertexArrays(1, VAO);
                    map.DisplayMap = VAO[0];
                    OpenGL.BindVertexArray(map.DisplayMap);
                    var VBO = new int[1];
                    OpenGL.GenBuffers(1, VBO);
                    OpenGL.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[0]);
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
                    OpenGL.BufferData(OpenGL.GL_ARRAY_BUFFER, bufSize, buf, OpenGL.GL_STATIC_DRAW);
                    Marshal.FreeHGlobal(buf);
                    var fields = elType.GetFields();
                    var offset = 0;
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var isLast = i == fields.Length - 1;
                        var next = isLast ? elSize : (int)Marshal.OffsetOf(elType, fields[i + 1].Name);
                        var size = (next - offset) / sizeof(float);
                        OpenGL.VertexAttribPointer(i, size, OpenGL.GL_FLOAT, false, elSize, (IntPtr)offset);
                        OpenGL.EnableVertexAttribArray(i);
                        offset = next;
                    }
                }
                OpenGL.BindVertexArray(map.DisplayMap);
                var uniformLoc = 0;
                OpenGL.UniformMatrix4fv(uniformLoc++, 1, false, ref obj.WorldTransform.M11); //do sprawdzenia
                if (map.Material != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        LoadTexture(map.Material.Textures[i], i);
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
                    OpenGL.UniformMatrix4fv(uniformLoc++, boneMatrix.Length, false, ref boneMatrix[0].M11);
                OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 3 * map.Faces.Count);
            }
            foreach (var child in obj.Children)
                DrawObject(child);
        }
        void LoadTexture(TMaterial.TTexture texture, int unit)
        {
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + unit);
            if (texture.DisplayList <= 0)
            {
                texture.DisplayList *= -1;
                var to = new int[] { texture.DisplayList };
                OpenGL.glDeleteTextures(1, to);
                OpenGL.glGenTextures(1, to);
                texture.DisplayList = to[0];
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture.DisplayList);
                var bmp = texture.Texture;
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, 4, bmp.Width, bmp.Height, 0, 
                    OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, bmpData.Scan0);
                OpenGL.GenerateMipmap(OpenGL.GL_TEXTURE_2D);
                bmp.UnlockBits(bmpData);
            }
            else
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture.DisplayList);
        }
    }
}
