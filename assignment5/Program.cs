using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace PhongOpenTK
{
    public class Window : GameWindow
    {
        private int _vao, _vbo;
        private Shader _shader = null!;
        private Camera _camera = null!;

        private readonly Vector3 _objectColor = new(0.2f, 0.4f, 0.9f);

        private Vector3 _lightPos = new(2.0f, 2.0f, 2.0f);
        private Vector3 _lightColor = new(1.0f, 1.0f, 1.0f);
        private float _lightIntensity = 1.0f;

        private static readonly float[] CubeVertices =
        {
            0.5f,-0.5f,-0.5f,  1,0,0,   0.5f, 0.5f,-0.5f, 1,0,0,   0.5f, 0.5f, 0.5f, 1,0,0,
            0.5f,-0.5f,-0.5f,  1,0,0,   0.5f, 0.5f, 0.5f, 1,0,0,   0.5f,-0.5f, 0.5f, 1,0,0,

           -0.5f,-0.5f,-0.5f, -1,0,0,  -0.5f,-0.5f, 0.5f,-1,0,0,  -0.5f, 0.5f, 0.5f,-1,0,0,
           -0.5f,-0.5f,-0.5f, -1,0,0,  -0.5f, 0.5f, 0.5f,-1,0,0,  -0.5f, 0.5f,-0.5f,-1,0,0,

           -0.5f, 0.5f,-0.5f,  0,1,0,  -0.5f, 0.5f, 0.5f, 0,1,0,   0.5f, 0.5f, 0.5f, 0,1,0,
           -0.5f, 0.5f,-0.5f,  0,1,0,   0.5f, 0.5f, 0.5f, 0,1,0,   0.5f, 0.5f,-0.5f, 0,1,0,

           -0.5f,-0.5f,-0.5f,  0,-1,0,   0.5f,-0.5f,-0.5f,0,-1,0,   0.5f,-0.5f, 0.5f,0,-1,0,
           -0.5f,-0.5f,-0.5f,  0,-1,0,   0.5f,-0.5f, 0.5f,0,-1,0,  -0.5f,-0.5f, 0.5f,0,-1,0,

           -0.5f,-0.5f, 0.5f,  0,0,1,   0.5f,-0.5f, 0.5f, 0,0,1,   0.5f, 0.5f, 0.5f, 0,0,1,
           -0.5f,-0.5f, 0.5f,  0,0,1,   0.5f, 0.5f, 0.5f, 0,0,1,  -0.5f, 0.5f, 0.5f, 0,0,1,

           -0.5f,-0.5f,-0.5f,  0,0,-1, -0.5f, 0.5f,-0.5f,0,0,-1,   0.5f, 0.5f,-0.5f,0,0,-1,
           -0.5f,-0.5f,-0.5f,  0,0,-1,  0.5f, 0.5f,-0.5f,0,0,-1,   0.5f,-0.5f,-0.5f,0,0,-1
        };

        public Window(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.Enable(EnableCap.DepthTest);

            _shader = new Shader("phong.vert", "phong.frag");
            _camera = new Camera(new Vector3(0, 0, 3), Size.X / (float)Size.Y);

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, CubeVertices.Length * sizeof(float), CubeVertices, BufferUsageHint.StaticDraw);

            int stride = 6 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            if (!IsFocused) return;

            var kb = KeyboardState;

            if (kb.IsKeyDown(Keys.Escape)) Close();

            float speed = 2.5f * (float)args.Time;
            if (kb.IsKeyDown(Keys.W)) _camera.Position += _camera.Front * speed;
            if (kb.IsKeyDown(Keys.S)) _camera.Position -= _camera.Front * speed;
            if (kb.IsKeyDown(Keys.A)) _camera.Position -= _camera.Right * speed;
            if (kb.IsKeyDown(Keys.D)) _camera.Position += _camera.Right * speed;
            if (kb.IsKeyDown(Keys.Q)) _camera.Position += _camera.Up * speed;
            if (kb.IsKeyDown(Keys.E)) _camera.Position -= _camera.Up * speed;

            if (kb.IsKeyPressed(Keys.Space))
                _lightPos = new Vector3(2.0f * MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds), 2.0f, 2.0f);

            _rotationAngle += 30f * (float)args.Time;
        }

        private float _rotationAngle = 0f;

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(0.07f, 0.09f, 0.12f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _shader.Use();

            Matrix4 model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_rotationAngle))
                          * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_rotationAngle * 0.5f));
            Matrix4 view = _camera.GetViewMatrix();
            Matrix4 proj = _camera.GetProjectionMatrix();

            Matrix3 normalMatrix = new Matrix3(model).Inverted();
            normalMatrix.Transpose();

            _shader.SetMatrix4("uModel", model);
            _shader.SetMatrix4("uView", view);
            _shader.SetMatrix4("uProjection", proj);
            _shader.SetMatrix3("uNormalMatrix", normalMatrix);

            _shader.SetVector3("uMaterial.ambient", _objectColor * 0.2f);
            _shader.SetVector3("uMaterial.diffuse", _objectColor);
            _shader.SetVector3("uMaterial.specular", new Vector3(0.6f, 0.6f, 0.6f));
            _shader.SetFloat("uMaterial.shininess", 32.0f);

            _shader.SetVector3("uLight.position", _lightPos);
            _shader.SetVector3("uLight.color", _lightColor * _lightIntensity);
            _shader.SetVector3("uLight.ambient", new Vector3(0.15f)); 
            _shader.SetVector3("uLight.diffuse", new Vector3(0.8f));
            _shader.SetVector3("uLight.specular", new Vector3(1.0f));

            _shader.SetVector3("uViewPos", _camera.Position);

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            if (_camera != null) _camera.Aspect = Size.X / (float)Size.Y;
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            _shader?.Dispose();
        }

        public static void Main()
        {
            var gws = GameWindowSettings.Default;
            var nws = new NativeWindowSettings()
            {
                Title = "Phong Lighting — OpenTK",
                Size = new Vector2i(1280, 800),
                Flags = ContextFlags.ForwardCompatible
            };
            using var win = new Window(gws, nws);
            win.Run();
        }
    }
}
