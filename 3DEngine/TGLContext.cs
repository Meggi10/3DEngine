using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGL;

namespace Diablo3DEngine
{
    public enum TDrawMode { Opaque, Transparent, Selection }
    public unsafe class TGLContext
    {
        public TGLView View;
        TDrawMode DrawMode;
        IntPtr HDC;
        IntPtr HRC;
        public TCamera Camera = new TCamera();
        public const int MAX_LIGHTS = 10;
        public const int MAX_BONES = 4;
        const int MAX_BONE_MATRICES = 1024;
        public bool IsInited;
        uint UboCamera;
        uint UboBones;
        uint UboLights;
        uint WoitFbo;
        uint OpaqueTexture;
        uint AccumTexture;
        uint RevealTexture;
        uint depthRbo;
        int woitW, woitH;
        uint RenderProgram, CompositeProgram;
        uint VAO;
        uint DepthRenderbuffer;

        public void CreateHandle()
        {
            HDC = View.CreateGraphics().GetHdc();
            var pfd = new Win32.PIXELFORMATDESCRIPTOR();
            var idx = Win32.ChoosePixelFormat(HDC, &pfd);
            Win32.SetPixelFormat(HDC, idx, &pfd);
            HRC = Win32.wglCreateContext(HDC);
            Win32.wglMakeCurrent(HDC, HRC);
            RenderProgram = OpenGL.CreateProgram();
            var vertexShader = OpenGL.CreateShader(OpenGL.GL_VERTEX_SHADER);
            OpenGL.CompileShader(vertexShader, ReadManifestText("Resources.vertexShader.glsl"));
            OpenGL.AttachShader(RenderProgram, vertexShader);
            var fragShader = OpenGL.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
            OpenGL.CompileShader(fragShader, ReadManifestText("Resources.fragShader.glsl"));
            OpenGL.AttachShader(RenderProgram, fragShader);
            OpenGL.LinkProgram(RenderProgram);
            //OpenGL.UseProgram(gpuProgram);
            CompositeProgram = OpenGL.CreateProgram();
            vertexShader = OpenGL.CreateShader(OpenGL.GL_VERTEX_SHADER);
            OpenGL.CompileShader(vertexShader, ReadManifestText("Resources.compositeVertex.glsl"));
            OpenGL.AttachShader(CompositeProgram, vertexShader);
            fragShader = OpenGL.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
            OpenGL.CompileShader(fragShader, ReadManifestText("Resources.compositeFragment.glsl"));
            OpenGL.AttachShader(CompositeProgram, fragShader);
            OpenGL.LinkProgram(CompositeProgram);
            uint rId;
            OpenGL.GenBuffers(1, &rId); UboCamera = rId;
            OpenGL.BufferData(OpenGL.GL_UNIFORM_BUFFER, sizeof(CameraUbo), null, OpenGL.GL_DYNAMIC_DRAW);
            OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 0, UboCamera);
            OpenGL.GenBuffers(1, &rId); UboBones = rId;
            OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 1, UboBones);
            OpenGL.GenBuffers(1, &rId); UboLights = rId;
            OpenGL.BindBufferBase(OpenGL.GL_UNIFORM_BUFFER, 2, UboLights);
        }
        public void Release()
        {
            Win32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Win32.wglDeleteContext(HRC);
        }

        //void InitWoitFramebuffer(int opaqueDepthBuffer)
        //{
        //    var width = View.Width;
        //    var height = View.Height;
        //    uint vao;
        //    OpenGL.GenVertexArrays(1, &vao);
        //    VAO = vao;
        //    uint woitFbo;
        //    OpenGL.GenFramebuffers(1, &woitFbo);
        //    WoitFbo = woitFbo;
        //    OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, WoitFbo);
        //    uint id;

        //    // 1. Tekstura Accumulation (RGBA16F dla wysokiej precyzji)
        //    OpenGL.GenTextures(1, &id);
        //    AccumTexture = id;
        //    OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, AccumTexture);
        //    OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_RGBA16F, width, height, 0, OpenGL.GL_RGBA, OpenGL.GL_HALF_FLOAT, null);
        //    OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_LINEAR);
        //    OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_LINEAR);
        //    OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, AccumTexture, 0);

        //    // 2. Tekstura Revealage (R8 lub R16F)
        //    OpenGL.GenTextures(1, &id);
        //    RevealTexture = id;
        //    OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, RevealTexture);
        //    OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_R16F, width, height, 0, OpenGL.GL_RED, OpenGL.GL_UNSIGNED_BYTE, null);
        //    OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_LINEAR);
        //    OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_LINEAR);
        //    OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT1, OpenGL.GL_TEXTURE_2D, RevealTexture, 0);

        //    // 3. Podpięcie współdzielonego bufora głębi (np. z FBO obiektów nieprzezroczystych)
        //    OpenGL.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT, OpenGL.GL_RENDERBUFFER, (uint)opaqueDepthBuffer);

        //    // 4. Określenie buforów wyjściowych (MRT)
        //    uint* buffers = stackalloc uint[] { OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_COLOR_ATTACHMENT1 };
        //    OpenGL.DrawBuffers(2, buffers);
        //    //if (OpenGL.CheckFramebufferStatus(OpenGL.GL_FRAMEBUFFER) != OpenGL.GL_FRAMEBUFFER_COMPLETE)
        //    //{
        //    //    throw new Exception("Błąd inicjalizacji WOIT Framebuffer!");
        //    //}
        //    OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
        //}
        void InitWoitFramebuffer()
        {
            var width = Math.Max(1, View.Width);
            var height = Math.Max(1, View.Height);

            uint vao;
            OpenGL.GenVertexArrays(1, &vao);
            VAO = vao;

            uint fbo;
            OpenGL.GenFramebuffers(1, &fbo);
            WoitFbo = fbo;
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, WoitFbo);

            // 0. Tekstura Opaque (ATTACHMENT0)
            uint tex;
            OpenGL.GenTextures(1, &tex);
            OpaqueTexture = tex;
            OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, OpaqueTexture);
            OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_RGBA8, width, height, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, null);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_LINEAR);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_LINEAR);
            OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, OpaqueTexture, 0);

            // 1. Tekstura Accumulation (ATTACHMENT1)
            OpenGL.GenTextures(1, &tex);
            AccumTexture = tex;
            OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, AccumTexture);
            OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_RGBA16F, width, height, 0, OpenGL.GL_RGBA, OpenGL.GL_HALF_FLOAT, null);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_LINEAR);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_LINEAR);
            OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT1, OpenGL.GL_TEXTURE_2D, AccumTexture, 0);

            // 2. Tekstura Revealage (ATTACHMENT2)
            OpenGL.GenTextures(1, &tex);
            RevealTexture = tex;
            OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, RevealTexture);
            OpenGL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_R16F, width, height, 0, OpenGL.GL_RED, OpenGL.GL_HALF_FLOAT, null);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_LINEAR);
            OpenGL.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_LINEAR);
            OpenGL.FramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT2, OpenGL.GL_TEXTURE_2D, RevealTexture, 0);

            // 3. Własny bufor głębi (DEPTH_ATTACHMENT)
            uint rbo;
            OpenGL.GenRenderbuffers(1, &rbo);
            DepthRenderbuffer = rbo;
            OpenGL.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, DepthRenderbuffer);
            //OpenGL.RenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_DEPTH_COMPONENT24, width, height);
            OpenGL.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, 0, OpenGL.GL_DEPTH_COMPONENT24, width, height);
            OpenGL.FramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT, OpenGL.GL_RENDERBUFFER, DepthRenderbuffer);

            uint* buffers = stackalloc uint[] 
            {
        OpenGL.GL_COLOR_ATTACHMENT0,
        OpenGL.GL_COLOR_ATTACHMENT1,
        OpenGL.GL_COLOR_ATTACHMENT2
    };
            OpenGL.DrawBuffers(3, buffers);

            //if (OpenGL.CheckFramebufferStatus(OpenGL.GL_FRAMEBUFFER) != OpenGL.GL_FRAMEBUFFER_COMPLETE)
            //    throw new Exception("Błąd inicjalizacji WOIT Framebuffer!");

            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
        }

        //Check helper method!
        public void EnsureWoit()
        {
            int w = View.Width;
            int h = View.Height;
            if (WoitFbo != 0 && w == woitW && h == woitH)
                return;

            // stare tekstury (FBO i renderbuffer)
            uint id;
            if (AccumTexture != 0) { id = AccumTexture; OpenGL.DeleteTextures(1, &id); }
            if (RevealTexture != 0) { id = RevealTexture; OpenGL.DeleteTextures(1, &id); }

            // renderbuffer głębi
            //OpenGL.GenRenderbuffers(1, &id);
            //depthRbo = id;
            //OpenGL.BindRenderbuffer(OpenGL.GL_RENDERBUFFER, depthRbo);
            //OpenGL.RenderbufferStorageMultisample(OpenGL.GL_RENDERBUFFER, 0, OpenGL.GL_DEPTH_COMPONENT24, w, h);

            InitWoitFramebuffer();
            woitW = w;
            woitH = h;
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
            Win32.wglMakeCurrent(HDC, HRC);
            Init();

            var vp = View.ClientRectangle;
            OpenGL.Viewport(vp.Left, vp.Top, vp.Width, vp.Height);
            var bg = View.BackColor;

            // 1. Aktywuj wspólne FBO oraz główny shader sceny!
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, WoitFbo);
            OpenGL.UseProgram(RenderProgram);

            // --- ETAP 1: OPAQUE (zapis do ATTACHMENT0 -> OpaqueTexture) ---
            DrawMode = TDrawMode.Opaque;
            uint att0 = OpenGL.GL_COLOR_ATTACHMENT0;
            OpenGL.DrawBuffers(1, &att0);

            OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
            OpenGL.DepthMask((byte)1);
            OpenGL.Disable(OpenGL.GL_BLEND);

            OpenGL.ClearColor(bg.R / 255f, bg.G / 255f, bg.B / 255f, 1f);
            OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            OpenGL.Uniform1i(5, 0); // uOIT = 0 (opaque)
            DrawScene();

            // --- ETAP 2: TRANSPARENT (zapis do ATTACHMENT1 -> Accum, ATTACHMENT2 -> Reveal) ---
            DrawMode = TDrawMode.Transparent;
            uint* atts = stackalloc uint[] { OpenGL.GL_COLOR_ATTACHMENT1, OpenGL.GL_COLOR_ATTACHMENT2 };
            OpenGL.DrawBuffers(2, atts);

            // Indeksy 0 i 1 w ClearBufferfv odnoszą się do tablicy 'atts' (czyli kolejno ATTACHMENT1 i ATTACHMENT2)
            float* zero = stackalloc float[] { 0f, 0f, 0f, 0f };
            float* one = stackalloc float[] { 1f, 1f, 1f, 1f };
            OpenGL.ClearBufferfv(OpenGL.GL_COLOR, 0, zero); // czyści AccumTexture
            OpenGL.ClearBufferfv(OpenGL.GL_COLOR, 1, one);  // czyści RevealTexture

            OpenGL.DepthMask((byte)0);
            OpenGL.Enable(OpenGL.GL_BLEND);
            OpenGL.BlendFunci(0, OpenGL.GL_ONE, OpenGL.GL_ONE);                  // buffer 0 (Accum): additive
            OpenGL.BlendFunci(1, OpenGL.GL_ZERO, OpenGL.GL_ONE_MINUS_SRC_COLOR); // buffer 1 (Reveal): *= (1 - a)

            OpenGL.Uniform1i(5, 1); // uOIT = 1 (transparent)
            DrawScene();

            // --- ETAP 3: COMPOSITE (złożenie na domyślny bufor ekranu = 0) ---
            OpenGL.Disable(OpenGL.GL_BLEND);
            OpenGL.DepthMask((byte)1);
            OpenGL.BindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            OpenGL.Disable(OpenGL.GL_DEPTH_TEST);

            OpenGL.UseProgram(CompositeProgram);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0); OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, OpaqueTexture);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 1); OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, AccumTexture);
            OpenGL.ActiveTexture(OpenGL.GL_TEXTURE0 + 2); OpenGL.BindTexture(OpenGL.GL_TEXTURE_2D, RevealTexture);

            OpenGL.BindVertexArray(VAO);
            OpenGL.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3);
            OpenGL.BindVertexArray(0);

            OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
            Win32.SwapBuffers(HDC);
        }

        void Init()
        {
            if (!IsInited)
            {
                OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
                IsInited = true;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct CameraUbo
        {
            public Matrix4x4 ViewProjectionMatrix; // offset 0  (64 bajty)
            public Vector3 CameraPos;              // offset 64 (16 bajtów: XYZ = pozycja, W = padding)
        }
        void InitCameraAndLights()
        {
            var buffer = new float[20];
            var cameraUbo = new CameraUbo();
            cameraUbo.ViewProjectionMatrix = Camera.Projection;
            cameraUbo.CameraPos = Camera.WorldTransform.Translation;
            OpenGL.BufferSubData(OpenGL.GL_UNIFORM_BUFFER, 0, sizeof(CameraUbo), &cameraUbo);
            var lightsCount = 0;
            buffer = new float[MAX_LIGHTS * 20 + 4];
            for (int i = 0; i < Camera.Scene.Lights.Count; i++)
            {
                var start = 20 * i;
                TLight light = Camera.Scene.Lights[i];
                if (!light.IsEnabled)
                    continue;
                lightsCount++;
                //light.WorldTransform = null;
                light.WorldTransform = light.Transform;
                while (light != Camera.Root && light.Parent != Camera.Root)
                    light.WorldTransform = light.Transform * light.Parent.WorldTransform;
                var position = light.WorldTransform.Translation;
                if (light.IsDirectional)
                {
                    position = Vector3.Normalize(position);
                    position *= -1;
                }
                else
                    buffer[start + 3] = 1;
                //Array.Copy(position.Data, 0, buffer, start, 3);
                buffer[start] = position.X;
                buffer[start + 1] = position.Y;
                buffer[start + 2] = position.Z;
                Array.Copy(light.AmbientLight.Data, 0, buffer, start + 4, 3);
                Array.Copy(light.DiffuseLight.Data, 0, buffer, start + 8, 3);
                Array.Copy(light.SpecularLight.Data, 0, buffer, start + 12, 3);
                Array.Copy(light.AttCoeff.Data, 0, buffer, start + 16, 3);
            }
            buffer[MAX_LIGHTS * 20] = lightsCount;
            OpenGL.BindBuffer(OpenGL.GL_UNIFORM_BUFFER, UboLights);
            fixed (float* buffPtr = &buffer[0])
            {
                OpenGL.BufferData(OpenGL.GL_UNIFORM_BUFFER, buffer.Length * sizeof(float), buffPtr, OpenGL.GL_STATIC_DRAW);
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
            InitCameraAndLights();
            DrawObject(Camera.Root);
        }

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
        public void DrawMap(TObject3D obj, TUvMap map)
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
                        for (int k = bones.Length - 1; k >= 0; k--)
                        {
                            var idx = bones.Length - 1 - k;
                            if (idx >= MAX_BONES)
                                break;
                            element.Bones[idx] = obj.Bones.IndexOf(bones[k]);
                            element.Weights[idx] = weights[k];
                        }
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
                OpenGL.UniformMatrix4fv(uniformLoc++, 1, 0, ptr);
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
            int boneCount = Math.Min(obj.Bones.Count, MAX_BONE_MATRICES);
            var boneMatrix = new Matrix4x4[Math.Max(boneCount, 1)];
            boneMatrix[0] = Matrix4x4.Identity;
            for (int i = 0; i < boneCount; i++)
            {
                var bone = obj.Bones[i];
                boneMatrix[i] = Matrix4x4.Multiply(bone.BindPoseInv, bone.WorldTransform);
            }
            OpenGL.BindBuffer(OpenGL.GL_UNIFORM_BUFFER, UboBones);
            if (boneMatrix.Length > 0)
                fixed (float* ptr = &boneMatrix[0].M11)
                {
                    OpenGL.BufferData(OpenGL.GL_UNIFORM_BUFFER, boneMatrix.Length * sizeof(Matrix4x4), ptr, OpenGL.GL_DYNAMIC_DRAW);
                    OpenGL.UniformMatrix4fv(uniformLoc++, boneMatrix.Length, 0, ptr);
                }
            OpenGL.DrawArrays(OpenGL.GL_TRIANGLES, 0, 3 * map.Faces.Count);
        }
        public void DrawObject(TObject3D obj)
        {
            if (DrawMode == TDrawMode.Opaque)
            {
                obj.WorldTransform = obj.Transform;
                if (obj != Camera.Root && obj.Parent != Camera.Root)
                    obj.WorldTransform = obj.Transform * obj.Parent.WorldTransform;
            }
            if (DrawMode == TDrawMode.Opaque && obj.Opaque.Value ||
                DrawMode == TDrawMode.Transparent && !obj.Opaque.Value)
                foreach (var map in obj.Maps)
                    DrawMap(obj, map);
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
