using System;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTKFpsCamera;
using StbImageSharp;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GameAssignment
{
    public class Game : GameWindow
    {
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _texture;
        private Shader _shader;
        private Camera _camera;
        private double _time;

        public Game(GameWindowSettings gwSettings, NativeWindowSettings nwSettings)
            : base(gwSettings, nwSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.1f, 0.12f, 0.15f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            CursorState = CursorState.Grabbed;

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _shader.Use();

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                Texture.CubeVertices.Length * sizeof(float),
                Texture.CubeVertices,
                BufferUsageHint.StaticDraw);

            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                Texture.CubeIndices.Length * sizeof(uint),
                Texture.CubeIndices,
                BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));

            _texture = LoadTexture("Resources/container.png");
            _camera = new Camera(Vector3.UnitZ * 5f);

            _shader.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.Uniform1(_shader.GetUniformLocation("uTex"), 0);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (IsFocused) 
            {
                var kb = KeyboardState;
                Vector2 move = Vector2.Zero;

                if (kb.IsKeyDown(Keys.W)) move.Y += 1f;
                if (kb.IsKeyDown(Keys.S)) move.Y -= 1f;
                if (kb.IsKeyDown(Keys.D)) move.X += 1f;
                if (kb.IsKeyDown(Keys.A)) move.X -= 1f;

                if (move.LengthSquared > 0f)
                {
                    if (move.Length > 1f)
                        move = move.Normalized();

                    _camera.ProcessKeyboard(move, (float)args.Time);
                }
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            // Exercise 3
            if (!IsFocused) return;
            _camera.ProcessMouseMove((float)e.Delta.X, (float)e.Delta.Y);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            // Exercise 4
            _camera.ProcessMouseScroll((float)e.OffsetY);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            _time += args.Time;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _shader.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.Uniform1(_shader.GetUniformLocation("uTex"), 0);

            var view = _camera.GetViewMatrix();
            float aspect = Size.X / (float)Size.Y;
            var proj = _camera.GetProjectionMatrix(aspect);
            GL.UniformMatrix4(_shader.GetUniformLocation("uView"), false, ref view);
            GL.UniformMatrix4(_shader.GetUniformLocation("uProj"), false, ref proj);

            GL.BindVertexArray(_vao);

            // Example 5
            for (int z = -5; z <= 5; z += 2)
            {
                for (int x = -5; x <= 5; x += 2)
                {
                    var model = Matrix4.CreateScale(0.9f)
                        * Matrix4.CreateRotationY((float)_time * 0.2f + (x + z))
                        * Matrix4.CreateTranslation(x, 0f, z);
                    GL.UniformMatrix4(_shader.GetUniformLocation("uModel"), false, ref model);
                    GL.DrawElements(PrimitiveType.Triangles, Texture.CubeIndices.Length, DrawElementsType.UnsignedInt, 0);
                }
            }

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteTexture(_texture);
            _shader?.Dispose();
        }

        private static int LoadTexture(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Texture not found: {path}");

            using var fs = File.OpenRead(path);
            var image = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);

            int handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, handle);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return handle;
        }
    }
}