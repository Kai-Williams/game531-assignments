using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Cube : GameWindow
{
    private int _vao, _vbo, _ebo;
    private int _shaderProgram;
    private int _texture;

    private float _autoRotationX = 0f;
    private float _autoRotationY = 0f;

    private float _manualRotationX = 0f;
    private float _manualRotationY = 0f;

    private bool _isDragging = false;
    private Vector2 _lastMousePos;

    private const float REPEAT = 3f;

    private readonly float[] vertices = {
        -0.5f, -0.5f,  0.5f, 0.0f, 0.0f,
         0.5f, -0.5f,  0.5f, 3.0f, 0.0f,
         0.5f,  0.5f,  0.5f, 3.0f, 3.0f,
        -0.5f,  0.5f,  0.5f, 0.0f,   3.0f,

         0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f, 3.0f, 0.0f,
        -0.5f,  0.5f, -0.5f, 3.0f, 3.0f,
         0.5f,  0.5f, -0.5f, 0.0f,   3.0f,

        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
        -0.5f, -0.5f,  0.5f, 3.0f, 0.0f,
        -0.5f,  0.5f,  0.5f, 3.0f, 3.0f,
        -0.5f,  0.5f, -0.5f, 0.0f,   3.0f,

         0.5f, -0.5f,  0.5f, 0.0f, 0.0f,
         0.5f, -0.5f, -0.5f, 3.0f, 0.0f,
         0.5f,  0.5f, -0.5f, 3.0f, 3.0f,
         0.5f,  0.5f,  0.5f, 0.0f,   3.0f,

        -0.5f,  0.5f,  0.5f, 0.0f, 0.0f,
         0.5f,  0.5f,  0.5f, 3.0f, 0.0f,
         0.5f,  0.5f, -0.5f, 3.0f, 3.0f,
        -0.5f,  0.5f, -0.5f, 0.0f,   3.0f,

        -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
         0.5f, -0.5f, -0.5f, 3.0f, 0.0f,
         0.5f, -0.5f,  0.5f, 3.0f, 3.0f,
        -0.5f, -0.5f,  0.5f, 0.0f,   3.0f,
    };

    private readonly uint[] indices = {
        0,1,2, 0,2,3,       
        4,5,6, 4,6,7,       
        8,9,10, 8,10,11,    
        12,13,14, 12,14,15, 
        16,17,18, 16,18,19, 
        20,21,22, 20,22,23  
    };

    public Cube(GameWindowSettings gws, NativeWindowSettings nws)
        : base(gws, nws) { }

    protected override void OnLoad()
    {
        GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
   
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

        string vertexShader = @"
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec2 aTexCoord;
            out vec2 vTexCoord;
            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;
            void main()
            {
                vTexCoord = aTexCoord;
                gl_Position = projection * view * model * vec4(aPosition, 1.0);
            }";

        string fragmentShader = @"
            #version 330 core
            in vec2 vTexCoord;
            out vec4 FragColor;
            uniform sampler2D uTexture;
            void main()
            {
                FragColor = texture(uTexture, vTexCoord);
            }";

        int vert = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vert, vertexShader);
        GL.CompileShader(vert);
        CheckShader(vert);

        int frag = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(frag, fragmentShader);
        GL.CompileShader(frag);
        CheckShader(frag);

        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vert);
        GL.AttachShader(_shaderProgram, frag);
        GL.LinkProgram(_shaderProgram);
        CheckProgram(_shaderProgram);

        GL.DeleteShader(vert);
        GL.DeleteShader(frag);

        _texture = LoadTexture("dogetexture.jpg");

        GL.UseProgram(_shaderProgram);
        GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "uTexture"), 0);

        CursorState = CursorState.Normal;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.UseProgram(_shaderProgram);

        var model = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_autoRotationX + _manualRotationX)) *
                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_autoRotationY + _manualRotationY));
        var view = Matrix4.CreateTranslation(0f, 0f, -3f);
        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), Size.X / (float)Size.Y, 0.1f, 100f);

        GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
        GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "view"), false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "projection"), false, ref projection);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture);

        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

        _autoRotationX += 30f * (float)args.Time;
        _autoRotationY += 50f * (float)args.Time;

        if (MouseState.IsButtonDown(MouseButton.Left))
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _lastMousePos = MouseState.Position;
            }
            else
            {
                var delta = MouseState.Position - _lastMousePos;
                _manualRotationY += delta.X * 0.5f;
                _manualRotationX += delta.Y * 0.5f;
                _lastMousePos = MouseState.Position;
            }
        }
        else
        {
            _isDragging = false;
        }
    }

    private int LoadTexture(string path)
    {
        int tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, tex);

        using (Bitmap bmp = new Bitmap(path))
        {
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                bmp.Width, bmp.Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);

            bmp.UnlockBits(data);
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        return tex;
    }

    private void CheckShader(int shader)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == 0)
            throw new Exception(GL.GetShaderInfoLog(shader));
    }

    private void CheckProgram(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
        if (status == 0)
            throw new Exception(GL.GetProgramInfoLog(program));
    }

    public static void Main()
    {
        var gws = GameWindowSettings.Default;
        var nws = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(800, 600),
        };
        using var win = new Cube(gws, nws);
        win.Run();
    }
}
