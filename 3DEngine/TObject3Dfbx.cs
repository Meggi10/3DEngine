using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace _3DEngine
{
    public class TObject3Dfbx : TObject3D
    {
        public static int Version;
        public string FilePath;
        List<string> TextureFiles = new List<string>();
        Dictionary<long, object> Connections = new Dictionary<long, object>();
        List<Vector2> TexVertices;
        string RefInf;
        public class TNode
        {
            public string Name;
            public long ID;
            public long StartPos, EndPos;
            public BinaryReader Reader;
            public object Owner;
            public TNode Parent;
            public delegate void TNodeReader(TNode node);
            public Dictionary<string, TNodeReader> SubNodes = new Dictionary<string, TNodeReader>();
            public void ReadHeader()
            {
                long propCount;
                long propSize;
                if (Version < 7500)
                {
                    EndPos = Reader.ReadInt32();
                    propCount = Reader.ReadInt32();
                    propSize = Reader.ReadInt32();
                }
                else
                {
                    EndPos = Reader.ReadInt64();
                    propCount = Reader.ReadInt64();
                    propSize = Reader.ReadInt64();
                }
                Name = Reader.ReadString();
                StartPos = (int)Reader.BaseStream.Position + propSize;
            }
            public void ReadSubNodes()
            {
                Reader.BaseStream.Position = StartPos;
                while(true)
                {
                    var subNode = new TNode();
                    subNode.Reader = Reader;
                    subNode.Owner = Owner;
                    subNode.Parent = this;
                    subNode.ReadHeader();
                    if (subNode.Name == "")
                        break;   
                    SubNodes.TryGetValue(subNode.Name, out TNodeReader nodeReader);
                    if (nodeReader != null) nodeReader(subNode);
                    Reader.BaseStream.Position = subNode.EndPos;
                }
            }
            public string ReadString()
            {
                var ch = Reader.ReadChar();
                var len = Reader.ReadInt32();
                return Encoding.UTF8.GetString(Reader.ReadBytes(len));
            }
            public int ReadInt()
            {
                var ch = Reader.ReadChar();
                return Reader.ReadInt32();
            }
            public long ReadLong()
            {
                var ch = Reader.ReadChar();
                return Reader.ReadInt64();
            }
            public double ReadDouble()
            {
                var ch = Reader.ReadChar();
                return Reader.ReadDouble();
            }
            public BinaryReader ReadArray()
            {
                var ch = Reader.ReadChar();
                var len = Reader.ReadInt32();
                var encoding = Reader.ReadInt32();
                var compressedLen = Reader.ReadInt32();
                var s = new MemoryStream();
                if (encoding == 0)
                    s.Write(Reader.ReadBytes(compressedLen), 0, compressedLen);
                else
                {
                    var dataBytes = Reader.ReadBytes(compressedLen);
                    var zipStream = new MemoryStream(dataBytes, 2, compressedLen - 6);
                    var inflated = new DeflateStream(zipStream, CompressionMode.Decompress);
                    inflated.CopyTo(s);
                }
                s.Position = 0;
                return new BinaryReader(s);
            }
        }
        void ReadObjects(TNode node)
        {
            node.SubNodes.Add("Model", ReadModel);
            node.SubNodes.Add("Geometry", ReadGeometry);
            node.SubNodes.Add("Material", ReadMaterial);
            node.SubNodes.Add("Texture", ReadTexture);
            ////node.SubNodes.Add("Pose", ReadPose);
            //node.SubNodes.Add("Deformer", ReadDeformer);
            //node.SubNodes.Add("AnimationStack", ReadAnimationStack);
            ////node.SubNodes.Add("AnimationLayer", ReadAnimationLayer);
            //node.SubNodes.Add("AnimationCurveNode", ReadAnimationCurveNode);
            //node.SubNodes.Add("AnimationCurve", ReadAnimationCurve);
            node.ReadSubNodes();
        }
        void ReadModel(TNode node)
        {
            node.ID = node.ReadLong();
            var obj = new TObject3D();
            obj.Name = node.ReadString();
            node.Owner = obj;
            Connections.Add(node.ID, node.Owner);
            node.SubNodes.Add("Properties70", ReadProperties);
            node.ReadSubNodes();
            //if (obj.Children.Count > 0)
            //    CorrectPreRotation(obj, obj.Children[0].Rotation);
        }
        void ReadGeometry(TNode node)
        {
            node.ID = node.ReadLong();
            var obj = new TObject3D();
            obj.Parent = this;
            obj.Maps = new List<TUvMap>();
            node.Owner = obj;
            Connections.Add(node.ID, node.Owner);
            node.SubNodes.Add("Vertices", ReadVertices);
            node.SubNodes.Add("PolygonVertexIndex", ReadFaces);
            node.SubNodes.Add("LayerElementUV", ReadFaceVertices);
            node.SubNodes.Add("LayerElementMaterial", ReadLayerMaterial);
            node.ReadSubNodes();
            obj.TesselateConvex();
        }
        void ReadVertices(TNode node)
        {
            var model = node.Owner as TObject3D;
            var reader = node.ReadArray();
            var len = reader.BaseStream.Length / 24;
            for (int i = 0; i < len; i++)
            {
                var v = new TVertex();
                v.Coords.X = (float)reader.ReadDouble();
                v.Coords.Y = (float)reader.ReadDouble();
                v.Coords.Z = (float)reader.ReadDouble();
                v.Index = i;// model.Vertices.Count;
                model.Vertices.Add(v);
            }
        }
        void ReadFaces(TNode node)
        {
            var model = node.Owner as TObject3D;
            var reader = node.ReadArray();
            var len = reader.BaseStream.Length / 4;
            var face = new TFace();
            model.Faces.Add(face);
            for (int i = 0; i < len; i++)
            {
                var idx = reader.ReadInt32();
                if (idx >= 0)
                    face.AddVertex(model.Vertices[idx]);
                else
                {
                    face.AddVertex(model.Vertices[~idx]);
                    face = new TFace();
                    model.Faces.Add(face);
                }
            }
            model.Faces.RemoveAt(model.Faces.Count - 1);
        }
        void ReadFaceVertices(TNode node)
        {
            var layer = node.ReadInt();
            var savePos = node.Reader.BaseStream.Position;
            node.Reader.BaseStream.Position = savePos;
            node.SubNodes.Add("UV", ReadUV);
            node.SubNodes.Add("UVIndex", ReadUVIndex);
            node.SubNodes.Add("ReferenceInformationType", ReadRefInf);
            node.ReadSubNodes();
            if (RefInf == "Direct")
            {
                var model = node.Owner as TObject3D;
                foreach(var face in model.Faces)
                {
                    for (int i = 0; i < face.Vertices.Count; i++)
                    {
                        var idx = face.Vertices[i].Index;
                        face.UV.Add(TexVertices[idx]);
                    }
                }
            }
            TexVertices.Clear();
        }
        void ReadRefInf(TNode node)
        {
            RefInf = node.ReadString();
        }
        void ReadLayerMaterial(TNode node)
        {
            var layer = node.ReadInt();
            //node.SubNodes.Add("MappingInformationType", ReadMapping);
            node.SubNodes.Add("Materials", ReadMaps);
            node.ReadSubNodes();

        }
        void ReadMaps(TNode node)
        {
            var model = node.Owner as TObject3D;
            var reader = node.ReadArray();
            var len = reader.BaseStream.Length / 4;
            if (len == 1)
            {
                var map = new TUvMap();
                model.Maps.Add(map);
                foreach (var face in model.Faces)
                    map.Faces.Add(face);
            }
            else
            {
                foreach (var face in model.Faces)
                {
                    var idx = reader.ReadInt32();
                    while (idx >= model.Maps.Count)
                        model.Maps.Add(new TUvMap());
                    model.Maps[idx].Faces.Add(face);
                }
            }
        }
        void ReadUV(TNode node)
        {
            var reader = node.ReadArray();
            var len = reader.BaseStream.Length / 16;
            TexVertices = new List<Vector2>();
            //var isDirect = RefInf == "Direct";
            for(int i = 0; i < len; i++)
            {
                var vt = new Vector2();
                vt.X = (float)reader.ReadDouble();
                vt.Y = (float)reader.ReadDouble();
                TexVertices.Add(vt);
            }
        }
        void ReadUVIndex(TNode node)
        {
            var model = node.Owner as TObject3D;
            var reader = node.ReadArray();
            for(int i = 0; i < model.Faces.Count; i++)
            {
                var face = model.Faces[i];
                for(int j = 0; j < face.Vertices.Count; j++)
                {
                    var idx = reader.ReadInt32();
                    face.UV.Add(TexVertices[idx]);
                }
            }
            TexVertices.Clear();
        }
        void ReadMaterial(TNode node)
        {
            node.ID = node.ReadLong();
            var mat = new TMaterial();
            Materials.Add(mat);
            mat.Name = node.ReadString();
            mat.Name = mat.Name.Split('\0')[0];
            mat.DiffuseMap.Path = TextureFiles.Find(x => Path.GetFileNameWithoutExtension(mat.Name) ==
            Path.GetFileNameWithoutExtension(x));
            node.Owner = mat;
            Connections.Add(node.ID, node.Owner);
            node.SubNodes.Add("Properties70", ReadProperties);
            node.ReadSubNodes();
        }
        void ReadProperties(TNode node)
        {
            node.SubNodes.Add("P", ReadProperty);
            node.ReadSubNodes();
        }
        void ReadProperty(TNode node)
        {
            var model = node.Owner as TObject3D;
            var name = node.ReadString();
            var type = node.ReadString();
            node.ReadString();
            node.ReadString();
            switch (name)
            {
                case "DiffuseColor":
                    {
                        var material = node.Owner as TMaterial;
                        var r = 255 * node.ReadDouble();
                        var g = 255 * node.ReadDouble();
                        var b = 255 * node.ReadDouble();
                        material.DiffuseMap.Color = Color.FromArgb((int)r, (int)g, (int)b);
                        break;
                    }
                //case "Lcl Scaling":
                //    {F
                //        model.Scale.X = (float)node.ReadDouble();
                //        model.Scale.Y = (float)node.ReadDouble();
                //        model.Scale.Z = (float)node.ReadDouble();
                //        break;
                //    }
                //case "Lcl Translation":
                //    {
                //        model.Origin.X = (float)node.ReadDouble();
                //        model.Origin.Y = (float)node.ReadDouble();
                //        model.Origin.Z = (float)node.ReadDouble();
                //        break;
                //    }
                //case "PreRotation":
                //    {
                //        var preObject = new TObject3D();
                //        preObject.Name = "PreObject";
                //        preObject.Parent = model;
                //        preObject.Rotation.X = node.ReadDouble();
                //        preObject.Rotation.Y = node.ReadDouble();
                //        preObject.Rotation.Z = node.ReadDouble();
                //        break;
                //    }
                //case "Lcl Rotation":
                //    {
                //        model.Rotation.X = node.ReadDouble();
                //        model.Rotation.Y = node.ReadDouble();
                //        model.Rotation.Z = node.ReadDouble();
                //        break;
                //    }
            }
        }
        void ReadTexture(TNode node)
        {
            node.ID = node.ReadLong();
            node.SubNodes.Add("FileName", ReadTextureFileName);
            node.ReadSubNodes();
        }
        void ReadConnections(TNode node)
        {
            node.SubNodes.Add("C", ReadConnection);
            node.ReadSubNodes();
        }
        void ReadConnection(TNode node)
        {
            var type = node.ReadString();
            var srcIdx = node.ReadLong();
            var dstIdx = node.ReadLong();
            Connections.TryGetValue(srcIdx, out object src);
            Connections.TryGetValue(dstIdx, out object dst);
            if (src is TMaterial && dst is TObject3D)
            {
                var srcMat = src as TMaterial;
                var dstObj = dst as TObject3D;
                dstObj.Materials.Add(srcMat);
                if (dstObj.Children.Count == 1)
                {
                    var idx = dstObj.Materials.Count - 1;
                    dstObj = dstObj.Children[0]; //dstObj is TMesh
                    if (idx < dstObj.Maps.Count)
                        dstObj.Maps[idx].Material = srcMat;
                }
            }
            else if (src is string texPath && dst is TMaterial)
            {
                var dstMat = dst as TMaterial;
                type = node.ReadString();
                if (type == "DiffuseColor")
                    dstMat.DiffuseMap.Path = texPath;
                else if (type == "SpecularColor")
                    dstMat.SpecularMap.Path = texPath;
                else if (type == "NormalMap")
                    dstMat.NormalMap.Path = texPath;
            }
            else if (src is TObject3D && dst is TObject3D)
            {
                var srcObj = src as TObject3D;
                var dstObj = dst as TObject3D;
                //if (srcObj.Parent != null)
                //    srcObj = srcObj.Copy();
                srcObj.Parent = dstObj;
            }
        }
        void ReadTextureFileName(TNode node)
        {
            var path = node.ReadString();
            var texturePath = TextureFiles.Find(x => Path.GetFileNameWithoutExtension(path) ==
            Path.GetFileNameWithoutExtension(x));
            //var texturePath = FilePath + Path.GetFileName(path);
            if (!Connections.ContainsKey(node.Parent.ID))
                Connections.Add(node.Parent.ID, texturePath);
        }
        public override TObject3D LoadFromStream(Stream s)
        {
            Connections.Add(0, this);
            //FilePath = Path.GetDirectoryName((s as FileStream).Name) + "/";
            TextureFiles.AddRange(Directory.GetFiles(FilePath, "*.jpg"));
            TextureFiles.AddRange(Directory.GetFiles(FilePath, "*.png"));
            var reader = new BinaryReader(s);
            var magic = "Kaydara FBX Binary  \x00";
            var header = new string(reader.ReadChars(magic.Length));
            if (header != magic) return null;
            reader.ReadBytes(2);
            Version = reader.ReadInt32();
            var root = new TNode();
            root.Reader = reader;
            root.StartPos = (int)reader.BaseStream.Position;
            root.SubNodes.Add("Objects", ReadObjects);
            root.SubNodes.Add("Connections", ReadConnections);
            root.ReadSubNodes();
            return this;
        }
    }
}
