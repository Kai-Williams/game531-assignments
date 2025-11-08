// File: Program.cs
//
// Fix explained: some OpenTK versions expose
// Matrix4.CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNear, float zFar)
// but older/newer API names may differ (e.g., near, far). Using *named* args can break across versions.
// We switch to *positional* args to be version-agnostic: CreateOrthographicOffCenter(0, 800, 0, 600, -1, 1).

using OpenTK.Graphics.OpenGL4;                       // OpenGL API
using OpenTK.Mathematics;                            // Matrix4, Vector types
using OpenTK.Windowing.Common;                       // Frame events (OnLoad/OnUpdate/OnRender)
using OpenTK.Windowing.Desktop;                      // GameWindow/NativeWindowSettings
using OpenTK.Windowing.GraphicsLibraryFramework;     // Keyboard state         
using System;
using System.IO;
using ImageSharp = SixLabors.ImageSharp.Image;       // Alias for brevity
using SixLabors.ImageSharp.PixelFormats;// Rgba32 pixel type

namespace OpenTK_Sprite_Animation 

{
    // --- Direction input abstraction -----------------------------------------------------------
    // Keeping this public so anything (like tests or future systems) can re-use it.
    public enum Direction { None, Right, Left }

    // Small, human-friendly enums to make logic readable at a glance.
    public enum Anim { Idle, Walk, Run, Jump }
    public enum Facing { Right = 1, Left = -1 }

    public class SpriteAnimationGame : GameWindow
    {
        private Character _character = null!;        // created in OnLoad; null-forgiven to please NRTs
        private int _shaderProgram;                   // Linked GLSL program
        private int _vao, _vbo;                       // Geometry
        private int _texture;

        public SpriteAnimationGame()
            : base(
                new GameWindowSettings(),
                // NOTE: newer OpenTK marks NativeWindowSettings.Size as obsolete; use ClientSize instead.
                new NativeWindowSettings { ClientSize = (800, 600), Title = "Sprite Animation" })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0f, 0f, 0f, 0f);            // Transparent background (A=0)
            GL.Enable(EnableCap.Blend);               // Enable alpha blending
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shaderProgram = CreateShaderProgram();   // Compile + link

            // We won’t bind a single atlas here anymore; Character swaps textures per-state.
            // Still, we keep a texture handle alive to avoid null binding surprises.
            _texture = GL.GenTexture();

            // Quad vertices: [pos.x, pos.y, uv.x, uv.y], centered model space
            float w = 64f, h = 64f;
            float[] vertices =
            {
                -w, -h, 0f, 0f,
                 w, -h, 1f, 0f,
                 w,  h, 1f, 1f,
                -w,  h, 0f, 1f
            };

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Attribute 0: vec2 position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            // Attribute 1: vec2 texcoord
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));

            GL.UseProgram(_shaderProgram);

            // Bind sampler to texture unit 0 (WHY: avoid undefined default binding)
            int texLoc = GL.GetUniformLocation(_shaderProgram, "uTexture");
            GL.Uniform1(texLoc, 0);

            // Orthographic projection (pixel coordinates 0..800, 0..600)
            // IMPORTANT: positional args to avoid API-name mismatch across OpenTK versions.
            int projLoc = GL.GetUniformLocation(_shaderProgram, "projection");
            Matrix4 ortho = Matrix4.CreateOrthographicOffCenter(0, 800, 0, 600, -1, 1);
            GL.UniformMatrix4(projLoc, false, ref ortho);

            // Model transform is driven by Character each frame; we just need the uniform location.
            int modelLoc = GL.GetUniformLocation(_shaderProgram, "model");

            // Create the character with separate strips: idle (single), walk, run, jump.
            _character = new Character(
                shader: _shaderProgram,
                modelUniform: modelLoc,
                offsetUniform: GL.GetUniformLocation(_shaderProgram, "uOffset"),
                sizeUniform: GL.GetUniformLocation(_shaderProgram, "uSize"),
                idlePath: "Woodcutter.png",
                walkPath: "Woodcutter_walk.png",
                runPath: "Woodcutter_run.png",
                jumpPath: "Woodcutter_jump.png"
            );

            // Start roughly on a “ground line”.
            _character.Position = new Vector2(400, 120);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            // Read keyboard state -> map to inputs
            var keyboard = KeyboardState;

            // Old abstraction kept: Direction is still here for left/right intent.
            Direction dir = Direction.None;
            if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D)) dir = Direction.Right;
            else if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A)) dir = Direction.Left;

            // Sprint/Run modifier feels better as a held key.
            bool sprint = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

            // Single-press jump to avoid key-repeat weirdness.
            bool jumpPressed = keyboard.IsKeyPressed(Keys.Space);

            // Feed the character. We keep the “hold last frame when stops” behavior inside Character.
            _character.Input(dir, sprint, jumpPressed);
            _character.Update((float)e.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Bind VAO then let Character bind whichever sheet it’s using.
            GL.BindVertexArray(_vao);
            _character.Render();

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            // Free GPU resources
            _character?.Dispose();
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteTexture(_texture);
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            base.OnUnload();
        }

        // --- Shader creation utilities ---------------------------------------------------------

        private int CreateShaderProgram()
        {
            // Vertex Shader: transforms positions, flips V in UVs (image origin vs GL origin)
            string vs = @"
#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
out vec2 vTexCoord;
uniform mat4 projection;
uniform mat4 model;
void main() {
    gl_Position = projection * model * vec4(aPosition, 0.0, 1.0);
    vTexCoord = vec2(aTexCoord.x, 1.0 - aTexCoord.y); // flip V so PNGs read intuitively
}";

            // Fragment Shader: samples sub-rect of the sheet using uOffset/uSize
            string fs = @"
#version 330 core
in vec2 vTexCoord;
out vec4 color;
uniform sampler2D uTexture; // bound to texture unit 0
uniform vec2 uOffset;       // normalized UV start (0..1)
uniform vec2 uSize;         // normalized UV size  (0..1)
void main() {
    vec2 uv = uOffset + vTexCoord * uSize;
    color = texture(uTexture, uv);
}";

            int v = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(v, vs);
            GL.CompileShader(v);
            CheckShaderCompile(v, "VERTEX");

            int f = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(f, fs);
            GL.CompileShader(f);
            CheckShaderCompile(f, "FRAGMENT");

            int p = GL.CreateProgram();
            GL.AttachShader(p, v);
            GL.AttachShader(p, f);
            GL.LinkProgram(p);
            CheckProgramLink(p);

            GL.DetachShader(p, v);
            GL.DetachShader(p, f);
            GL.DeleteShader(v);
            GL.DeleteShader(f);

            return p;
        }

        private static void CheckShaderCompile(int shader, string stage)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0)
                throw new Exception($"{stage} SHADER COMPILE ERROR:\n{GL.GetShaderInfoLog(shader)}");
        }

        private static void CheckProgramLink(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int ok);
            if (ok == 0)
                throw new Exception($"PROGRAM LINK ERROR:\n{GL.GetProgramInfoLog(program)}");
        }

        // --- Texture loading ------------------------------------------------------------------

        public static int LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                // fallback to /mnt/data for environments that mount assets there
                string alt = Path.Combine("/mnt/data", Path.GetFileName(path));
                if (File.Exists(alt)) path = alt;
                else throw new FileNotFoundException($"Texture not found: {path}");
            }

            using var img = ImageSharp.Load<Rgba32>(path); 

            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            var pixels = new byte[4 * img.Width * img.Height];
            img.CopyPixelDataTo(pixels);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          img.Width, img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            return tex;
        }
    }

    public class Character : IDisposable
    {
        private readonly int _shader;   
        private readonly int _uModel, _uOffset, _uSize;

        private float _timer;           
        private int _frame;             
        private Direction _dirInput;    

        // Timing
        private const float WalkFT = 0.10f;
        private const float RunFT = 0.07f;
        private const float JumpFT = 0.085f;

        // Sprite strip facts (48x48 frames, 6-wide for strips)
        private const int FrameW = 48;
        private const int FrameH = 48;
        private const int StripFrames = 6;

        // Movement/physics
        public Vector2 Position;
        public Facing Facing = Facing.Right;
        private float _vx, _vy;
        private bool _grounded = true;
        private const float WalkSpeed = 110f;
        private const float RunSpeed = 200f;
        private const float JumpVel = 320f;
        private const float Gravity = -900f;
        private const float GroundY = 120f;

        // State machine
        private Anim _anim = Anim.Idle;
        private bool _wantRun;
        private bool _wantJump;

        // Textures (separate strips keep it clean)
        private readonly int _texIdle, _texWalk, _texRun, _texJump;

        public Character(int shader, int modelUniform, int offsetUniform, int sizeUniform,
                         string idlePath, string walkPath, string runPath, string jumpPath)
        {
            _shader = shader; _uModel = modelUniform; _uOffset = offsetUniform; _uSize = sizeUniform;

            // Load once; reuse every frame.
            _texIdle = SpriteAnimationGame.LoadTexture(idlePath);
            _texWalk = SpriteAnimationGame.LoadTexture(walkPath);
            _texRun = SpriteAnimationGame.LoadTexture(runPath);
            _texJump = SpriteAnimationGame.LoadTexture(jumpPath);
        }

        public void Input(Direction dir, bool sprintHeld, bool jumpPressed)
        {
            _dirInput = dir;
            _wantRun = sprintHeld;

            if (jumpPressed) _wantJump = true; // handled in Update so we don’t miss frames
        }

        public void Update(float dt)
        {
            // Jump 
            if (_wantJump && _grounded)
            {
                _grounded = false;
                _vy = JumpVel;
                SwitchAnim(Anim.Jump);
            }
            _wantJump = false;

            // Horizontal velocity target
            float target = 0f;
            if (_dirInput == Direction.Left) target = -(_wantRun ? RunSpeed : WalkSpeed);
            if (_dirInput == Direction.Right) target = (_wantRun ? RunSpeed : WalkSpeed);

            // A little smoothing feels to make it look better
            _vx = MathHelper.Lerp(_vx, target, 0.25f);

            // Facing flips visually 
            if (_dirInput == Direction.Left) Facing = Facing.Left;
            if (_dirInput == Direction.Right) Facing = Facing.Right;

            // Vertical motion (simple gravity)
            if (!_grounded) _vy += Gravity * dt;

            // Integrate motion
            Position.X += _vx * dt;
            Position.Y += _vy * dt;

            if (Position.Y <= GroundY)
            {
                Position.Y = GroundY;
                _vy = 0f;
                _grounded = true;
            }

            // Choose animation when on ground
            if (_grounded)
            {
                if (MathF.Abs(_vx) < 5f) SwitchAnim(Anim.Idle);
                else SwitchAnim(_wantRun ? Anim.Run : Anim.Walk);
            }

            // Advance frames only while state is “active”
            float ft = _anim switch
            {
                Anim.Walk => WalkFT,
                Anim.Run => RunFT,
                Anim.Jump => JumpFT,
                _ => 0.2f
            };

            bool shouldAdvance =
                _anim == Anim.Jump ||  // airborne animation will always runs
                (_anim == Anim.Run && MathF.Abs(_vx) >= 5f) ||
                (_anim == Anim.Walk && MathF.Abs(_vx) >= 5f);

            if (shouldAdvance)
            {
                _timer += dt;
                if (_timer >= ft)
                {
                    _timer -= ft;
                    _frame = (_anim == Anim.Idle) ? 0 : (_frame + 1) % StripFrames;
                }
            }

        }

        public void Render()
        {
            // Bind the right sheet for the current state.
            int tex = _anim switch
            {
                Anim.Walk => _texWalk,
                Anim.Run => _texRun,
                Anim.Jump => _texJump,
                _ => _texIdle
            };
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);

            // Model transform: place + mirror on X if facing left
            float flipX = (Facing == Facing.Left) ? -1f : 1f;
            Matrix4 model =
                Matrix4.CreateScale(flipX, 1f, 1f) *
                Matrix4.CreateTranslation(new Vector3(Position.X, Position.Y, 0f));
            GL.UniformMatrix4(_shader.GetUniformLocation("model"), false, ref model); // a safety if called directly
            GL.UniformMatrix4(_uModel, false, ref model);

            if (_anim == Anim.Idle)
                UploadUV(0, 0, FrameW, FrameH, FrameW, FrameH);
            else
            {
                int sheetW = FrameW * StripFrames;
                int x = _frame * FrameW;
                UploadUV(x, 0, FrameW, FrameH, sheetW, FrameH);
            }

            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4); // Draw quad
        }

        private void SwitchAnim(Anim a)
        {
            if (_anim == a) return;
            _anim = a;
            _frame = 0;
            _timer = 0f;
        }

        private void UploadUV(int px, int py, int pw, int ph, int sheetW, int sheetH)
        {
            float u = (float)px / sheetW;
            float v = (float)py / sheetH;
            float w = (float)pw / sheetW;
            float h = (float)ph / sheetH;

            GL.Uniform2(_uOffset, u, v);
            GL.Uniform2(_uSize, w, h);
        }

        public void Dispose()
        {
            GL.DeleteTexture(_texIdle);
            GL.DeleteTexture(_texWalk);
            GL.DeleteTexture(_texRun);
            GL.DeleteTexture(_texJump);
        }
    }

    // --- Entry point ---------------------------------------------------------------------------
    internal class Program
    {
        private static void Main()
        {
            using var game = new SpriteAnimationGame(); // Ensures Dispose/OnUnload is called
            game.Run();                                  // Game loop: Load -> (Update/Render)* -> Unload
        }
    }

    // --- Extension methods for GL calls -------------------------------------------------------
    internal static class GLProgExtensions
    {
        public static int GetUniformLocation(this int program, string name)
            => GL.GetUniformLocation(program, name);
    }
}
