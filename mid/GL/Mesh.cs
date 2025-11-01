using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace midtermGame.Graphics
{
    // Utility mesh class that uploads simple geometry to GPU.
    // Creates common shapes (UV sphere, sky cube, quad, ring) used by the renderer.

    public sealed class Mesh : IDisposable
    {
        public struct Vertex
        {
            public Vector3 Pos;
            public Vector3 Normal;
            public Vector2 UV;
        }

        int _vao, _vbo, _ebo;
        int _indexCount;

        public static Mesh CreateUvSphere(int lat = 48, int lon = 64, float radius = 1f)
        {
            var verts = new List<Vertex>();
            var inds = new List<int>();

            for (int y = 0; y <= lat; y++)
            {
                float v = y / (float)lat;
                float theta = v * MathF.PI;

                for (int x = 0; x <= lon; x++)
                {
                    float u = x / (float)lon;
                    float phi = u * MathF.PI * 2f;

                    var n = new Vector3(
                        MathF.Sin(theta) * MathF.Cos(phi),
                        MathF.Cos(theta),
                        MathF.Sin(theta) * MathF.Sin(phi));

                    verts.Add(new Vertex
                    {
                        Pos = n * radius,
                        Normal = n,
                        UV = new Vector2(u, 1f - v)
                    });
                }
            }

            int stride = lon + 1;
            for (int y = 0; y < lat; y++)
            {
                for (int x = 0; x < lon; x++)
                {
                    int i0 = y * stride + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + stride;
                    int i3 = i2 + 1;

                    inds.Add(i0); inds.Add(i1); inds.Add(i2);
                    inds.Add(i1); inds.Add(i3); inds.Add(i2);
                }
            }

            var m = new Mesh();
            m.Upload(verts.ToArray(), inds.ToArray());
            return m;
        }

        public static Mesh CreateSkyCube(float half = 1f)
        {
            var v = new List<Vertex>();
            var idx = new List<int>();

            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int start = v.Count;
                v.Add(new Vertex { Pos = a * half, Normal = n, UV = new Vector2(0, 0) });
                v.Add(new Vertex { Pos = b * half, Normal = n, UV = new Vector2(1, 0) });
                v.Add(new Vertex { Pos = c * half, Normal = n, UV = new Vector2(1, 1) });
                v.Add(new Vertex { Pos = d * half, Normal = n, UV = new Vector2(0, 1) });
                idx.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            Face(new Vector3(1, -1, -1), new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, -1), new Vector3(1, 0, 0));
            Face(new Vector3(-1, -1, 1), new Vector3(-1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, 1, 1), new Vector3(-1, 0, 0));
            Face(new Vector3(-1, 1, -1), new Vector3(1, 1, -1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(0, 1, 0));
            Face(new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, -1, -1), new Vector3(-1, -1, -1), new Vector3(0, -1, 0));
            Face(new Vector3(1, -1, 1), new Vector3(-1, -1, 1), new Vector3(-1, 1, 1), new Vector3(1, 1, 1), new Vector3(0, 0, 1));
            Face(new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1), new Vector3(0, 0, -1));

            var m = new Mesh();
            m.Upload(v.ToArray(), idx.ToArray());
            return m;
        }

        public static Mesh CreateQuad(float half = 1f)
        {
            var v = new[]
            {
                new Vertex{ Pos=new Vector3(-half, -half, 0), Normal=Vector3.UnitZ, UV=new Vector2(0,0)},
                new Vertex{ Pos=new Vector3( half, -half, 0), Normal=Vector3.UnitZ, UV=new Vector2(1,0)},
                new Vertex{ Pos=new Vector3( half,  half, 0), Normal=Vector3.UnitZ, UV=new Vector2(1,1)},
                new Vertex{ Pos=new Vector3(-half,  half, 0), Normal=Vector3.UnitZ, UV=new Vector2(0,1)},
            };
            var idx = new[] {0,1,2, 0,2,3};
            var m = new Mesh(); m.Upload(v, idx); return m;
        }

        public static Mesh CreateRing(int segments = 128, float inner = 0.5f, float outer = 1.0f)
        {
            if (segments < 3) segments = 3;
            var verts = new List<Vertex>();
            var inds = new List<int>();

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float theta = t * MathF.PI * 2f;
                float c = MathF.Cos(theta);
                float s = MathF.Sin(theta);

                verts.Add(new Vertex
                {
                    Pos = new Vector3(c * inner, 0f, s * inner),
                    Normal = Vector3.UnitY,
                    UV = new Vector2(t, 0f)
                });

                verts.Add(new Vertex
                {
                    Pos = new Vector3(c * outer, 0f, s * outer),
                    Normal = Vector3.UnitY,
                    UV = new Vector2(t, 1f)
                });
            }

            for (int i = 0; i < segments; i++)
            {
                int i0 = i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;

                inds.Add(i0); inds.Add(i2); inds.Add(i1);
                inds.Add(i1); inds.Add(i2); inds.Add(i3);
            }

            var m = new Mesh();
            m.Upload(verts.ToArray(), inds.ToArray());
            return m;
        }

        void Upload(Vertex[] vertices, int[] indices)
        {
            _indexCount = indices.Length;

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            int vSize = vertices.Length * Marshal.SizeOf<Vertex>();
            GL.BufferData(BufferTarget.ArrayBuffer, vSize, vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

            int stride = (3 + 3 + 2) * sizeof(float);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));

            GL.BindVertexArray(0);
        }

        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            if (_ebo != 0) GL.DeleteBuffer(_ebo);
            if (_vbo != 0) GL.DeleteBuffer(_vbo);
            if (_vao != 0) GL.DeleteVertexArray(_vao);
            _ebo = _vbo = _vao = 0;
        }
    }
}
