using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class cube : GameWindow
{
    private int _vao, _vbo, _ebo;
    private Shader _shader;

    
    private float _autoRotationX = 0f;
    private float _autoRotationY = 0f;

    
    private float _manualRotationX = 0f;
    private float _manualRotationY = 0f;

    private bool _isDragging = false;
    private Vector2 _lastMousePos;

    
    private readonly float[] vertices = {
        -0.5f, -0.5f, -0.5f, 
         0.5f, -0.5f, -0.5f, 
         0.5f,  0.5f, -0.5f, 
        -0.5f,  0.5f, -0.5f, 
        -0.5f, -0.5f,  0.5f, 
         0.5f, -0.5f,  0.5f, 
         0.5f,  0.5f,  0.5f, 
        -0.5f,  0.5f,  0.5f  
    };

    private readonly uint[] indices = {
        0,1,2, 2,3,0,
        4,5,6, 6,7,4,
        0,4,7, 7,3,0,
        1,5,6, 6,2,1,
        0,1,5, 5,4,0,
        3,2,6, 6,7,3
    };

    public cube(GameWindowSettings gws, NativeWindowSettings nws)
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

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        string vertexShader = @"
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;
            void main()
            {
                gl_Position = projection * view * model * vec4(aPosition, 1.0);
            }";

        string fragmentShader = @"
    #version 330 core
    out vec4 FragColor;
    void main()
    {
        FragColor = vec4(0.6, 0.2, 0.8, 1.0); // purple
    }";

        _shader = new Shader(vertexShader, fragmentShader);

        CursorState = CursorState.Normal;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _shader.Use();

       
        var model = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_autoRotationX + _manualRotationX)) *
                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_autoRotationY + _manualRotationY));

        var view = Matrix4.CreateTranslation(0f, 0f, -3f);
        var projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(60f),
            Size.X / (float)Size.Y,
            0.1f, 100f
        );

        _shader.SetMatrix4("model", model);
        _shader.SetMatrix4("view", view);
        _shader.SetMatrix4("projection", projection);

        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape))
            Close();

        
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
}
