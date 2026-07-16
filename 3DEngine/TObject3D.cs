using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace _3DEngine
{
    [Serializable]
    public class TObject3D
    {
        public List<TVertex> Vertices = new List<TVertex>();
        public List<TFace> Faces = new List<TFace>();
        public List<TMaterial> Materials = new List<TMaterial>();
        public List<TObject3D> Bones = new List<TObject3D>();
        public Matrix4x4 BindPoseInv = new Matrix4x4();
        public string Name;
        public TObject3D Root
        {
            get
            {
                var root = this;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }
        }
        List<TUvMap> maps;
        public List<TUvMap> Maps
        {
            get 
            {
                if (maps == null)
                {
                    maps = new List<TUvMap>();
                    foreach (var face in Faces)
                    {
                        var map = maps.Find(m => m.Material == face.Material);
                        if (map == null)
                        {
                            map = new TUvMap();
                            map.Material = face.Material;
                            maps.Add(map);
                        }
                        map.Faces.Add(face);
                    }
                }
                return maps;
            }
            set
            {
                maps = value;
            }
        }
        Vector3 scale = new Vector3(1, 1, 1);
        public Vector3 Scale
        {
            get => scale;
            set { scale = value; IsValidTransform = false; }
        }
        Vector3 shear = new Vector3();
        public Vector3 Shear
        {
            get => shear;
            set { shear = value; IsValidTransform = false; }
        }
        Quaternion rotation = new Quaternion(0, 0, 0, 1);
        public Quaternion Rotation
        {
            get => rotation;
            set { rotation = value; IsValidTransform = false; }
        }
        Vector3 origin = new Vector3();
        public Vector3 Origin
        {
            get => origin;
            set { origin = value; IsValidTransform = false; }
        }
        bool IsValidTransform;
        Matrix4x4 transform;
        public Matrix4x4 Transform
        {
            get
            {
                if (!IsValidTransform)
                {
                    transform = Matrix4x4.CreateFromQuaternion(Rotation);
                    transform = transform.ScaleFast(Scale);
                    transform = transform.TranslateFast(Origin);
                    IsValidTransform = true;
                }
                return transform;
            }
        }
        public Matrix4x4 WorldTransform;
        public List<TObject3D> Children = new List<TObject3D>();
        TObject3D parent;
        public TObject3D Parent
        {
            get { return parent; }
            set
            {
                if (parent != null)
                    parent.Children.Remove(this);
                parent = value;
                if (parent != null)
                    parent.Children.Add(this);
            }
        }
        public static TObject3D CreateCube()
        {
            var obj = new TObject3D();
            var mat = new TMaterial();
            obj.Materials.Add(mat);
            var lbn = new Vector3(-1, -1, -1);
            var rtf = new Vector3(1, 1, 1);
            for (int i = 0; i < 8; i++)
            {
                var v = new TVertex();
                v.Coords = lbn;
                //for (int j = 0; j < 3; j++)
                //    if ((i & 1 << j) != 0)
                //        v.Coords[j] = rtf[j];
                if ((i & 1) != 0)
                    v.Coords.X = rtf.X;
                if ((i & 2) != 0)
                    v.Coords.Y = rtf.Y;
                if ((i & 4) != 0)
                    v.Coords.Z = rtf.Z;
                obj.Vertices.Add(v);
            }
            var faces = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                var axis1 = 1 << i;
                var axis2 = 1 << (i + 1) % 3;
                faces.Add(0);
                faces.Add(axis1);
                faces.Add(axis1 | axis2);
                faces.Add(axis1 | axis2);
                faces.Add(axis2);
                faces.Add(0);
            }
            var count = faces.Count;
            for (int i = 0; i < count; i += 3)
                for (int j = 2; j >= 0; j--)
                    faces.Add(7 - faces[i + j]);
            for (int i = 0; i < faces.Count; i += 6)
            {
                var faceA = new TFace();
                faceA.AddVertex(obj.Vertices[faces[i + 0]]);
                faceA.AddVertex(obj.Vertices[faces[i + 1]]);
                faceA.AddVertex(obj.Vertices[faces[i + 2]]);
                faceA.Material = mat;
                obj.Faces.Add(faceA);

                var faceB = new TFace();
                faceB.AddVertex(obj.Vertices[faces[i + 3]]);
                faceB.AddVertex(obj.Vertices[faces[i + 4]]);
                faceB.AddVertex(obj.Vertices[faces[i + 5]]);
                faceB.Material = mat;
                obj.Faces.Add(faceB);

                faceA.UV.Add(new Vector2(0, 0));
                faceA.UV.Add(new Vector2(1, 0));
                faceA.UV.Add(new Vector2(1, 1));
                faceB.UV.Add(new Vector2(1, 1));
                faceB.UV.Add(new Vector2(0, 1));
                faceB.UV.Add(new Vector2(0, 0));
            }
            return obj;
        }
        public virtual void SaveToStream(Stream s)
        {
            var bf = new BinaryFormatter();
            bf.Serialize(s, this);
        }
        public virtual TObject3D LoadFromStream(Stream s)
        {
            var bf = new BinaryFormatter();
            return (TObject3D)bf.Deserialize(s);
        }
        public TObject3D LoadFromFile(string fileName)
        {
            var s = new FileStream(fileName, FileMode.Open);
            var obj = LoadFromStream(s);
            s.Close();
            return obj;
        }
        public void TesselateConvex()
        {
            foreach(var map in Maps)
            {
                var faces = map.Faces;
                map.Faces = new List<TFace>();
                foreach (var face in faces)
                    for (int i = 2; i < face.Vertices.Count; i++)
                    {
                        var triangle = new TFace();
                        triangle.AddVertex(face.Vertices[0]);
                        triangle.AddVertex(face.Vertices[i - 1]);
                        triangle.AddVertex(face.Vertices[i]);
                        if (face.UV.Count > 0)
                        {
                            triangle.UV = new List<Vector2>();
                            triangle.UV.Add(face.UV[0]);
                            triangle.UV.Add(face.UV[i - 1]);
                            triangle.UV.Add(face.UV[i]);
                        }
                        triangle.Material = face.Material;
                        map.Faces.Add(triangle);
                    }
            }
        }
    }
}
