using System;
using System.Collections.Generic;
using System.IO;
using midtermGame.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace midtermGame
{
    // Main game window and update/render loop.
    // This class connects input, camera, planets, and all drawing logic.
    // Note: smooth dragging, camera sync, and ray-plane intersections take careful math to avoid “snapping.”
    public class Game : GameWindow
    {
        Camera _cam = new(Vector3.UnitZ * 120 + Vector3.UnitY * 20);
        Shader _shader;
        Mesh _skyCube;
        Texture _skyTex;

        Mesh _saturnRingMesh;
        Texture _saturnRingTex;

        const float BoxHalfSize = 120f;
        const float CameraCollisionRadius = 1.0f;

        readonly List<Planet> _planets = new();

        // Drag state
        Planet _dragging;
        Vector3 _dragOffset;
        Vector3 _dragPlaneN;
        float _dragPlaneD;
        float _dragSmoothK = 36f; // higher = snappier

        Vector2 _dragStartMouse;
        Vector3 _dragPlaneHitStart;
        Vector3 _dragPerPixelX;
        Vector3 _dragPerPixelY;
        float _dragSavedOrbitSpeed = 0f;

        bool _cursorLocked = true;
        Vector2 _lastMouse;

        Planet _sun, _earth, _moon;
        float _lightIntensity = 3.5f;
        bool _lightOn = true;

        public Game()
        : base(GameWindowSettings.Default, new NativeWindowSettings
        {
            Title = "Solar System — drag the planets",
            Size = new Vector2i(1280, 720)
        })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            // Basic OpenGL setup
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.FramebufferSrgb);
            GL.FrontFace(FrontFaceDirection.Ccw);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Load shaders
            var shaderDir = Path.Combine(AppContext.BaseDirectory, "Shaders");
            string vertexSrc = File.ReadAllText(Path.Combine(shaderDir, "vert.glsl"));
            string fragmentSrc = File.ReadAllText(Path.Combine(shaderDir, "frag.glsl"));
            _shader = Shader.FromSource(vertexSrc, fragmentSrc);

            // Skybox setup
            _skyCube = Mesh.CreateSkyCube(1f);
            _skyTex = Texture.Load2D(Path.Combine("Images", "8k_stars_milky_way.jpg"));

            // Load textures for all planets
            var sunTex = Texture.Load2D(Path.Combine("Images", "8k_sun.jpg"), srgb: true);
            var earthTex = Texture.Load2D(Path.Combine("Images", "8k_earth_daymap.jpg"), srgb: true);
            var moonTex = Texture.Load2D(Path.Combine("Images", "8k_moon.jpg"), srgb: true);
            var marsTex = Texture.Load2D(Path.Combine("Images", "8k_mars.jpg"), srgb: true);
            var jupiterTex = Texture.Load2D(Path.Combine("Images", "8k_jupiter.jpg"), srgb: true);
            var saturnTex = Texture.Load2D(Path.Combine("Images", "8k_saturn.jpg"), srgb: true);
            var venusTex = Texture.Load2D(Path.Combine("Images", "4k_venus_atmosphere.jpg"), srgb: true);
            var uranusTex = Texture.Load2D(Path.Combine("Images", "2k_uranus.jpg"), srgb: true);
            var neptuneTex = Texture.Load2D(Path.Combine("Images", "2k_neptune.jpg"), srgb: true);

            // Create planets with realistic sizes, orbits, and colors.
            var sun = new Planet("Sun", sunTex, 5f, Vector3.Zero)
            {
                SelfRotate = 5f,
                AtmosphereTint = new Vector3(1.0f, 0.95f, 0.9f)
            };
            _sun = sun;

            var venus = new Planet("Venus", venusTex, 1.9f, new Vector3(20, 0, 0))
            {
                OrbitCenter = _sun.Position,
                OrbitRadius = 20,
                OrbitSpeed = 17f,
                SelfRotate = 10f,
                AtmosphereTint = new Vector3(1.0f, 0.9f, 0.6f)
            };

            var earth = new Planet("Earth", earthTex, 2f, new Vector3(35, 0, 0))
            {
                OrbitCenter = _sun.Position,
                OrbitRadius = 35,
                OrbitSpeed = 10f,
                SelfRotate = 25f,
                AtmosphereTint = new Vector3(0.5f, 0.7f, 1.0f)
            };
            _earth = earth;

            var moon = new Planet("Moon", moonTex, 0.6f, new Vector3(38, 0, 0))
            {
                OrbitCenter = earth.Position,
                OrbitRadius = 3f,
                OrbitSpeed = 100f,
                SelfRotate = 10f,
                AtmosphereTint = new Vector3(0.8f, 0.8f, 0.85f)
            };
            _moon = moon;

            var mars = new Planet("Mars", marsTex, 1.6f, new Vector3(50, 0, 0))
            {
                OrbitCenter = _sun.Position,
                OrbitRadius = 50,
                OrbitSpeed = 10f,
                SelfRotate = 20f,
                AtmosphereTint = new Vector3(1.0f, 0.5f, 0.3f)
            };

            var jupiter = new Planet("Jupiter", jupiterTex, 5.6f, new Vector3(70, 0, 0))
            {
                OrbitCenter = _sun.Position,
                OrbitRadius = 70,
                OrbitSpeed = 5f,
                SelfRotate = 15f,
                AtmosphereTint = new Vector3(1.0f, 0.95f, 0.9f)
            };

            var saturn = new Planet("Saturn", saturnTex, 4.8f, new Vector3(90, 0, 0))
            {
                OrbitCenter = _sun.Position,
                OrbitRadius = 90,
                OrbitSpeed = 4f,
                SelfRotate = 15f,
                AtmosphereTint = new Vector3(1.0f, 0.9f, 0.6f)
            };

            var uranus = new Planet("Uranus", uranusTex, 3.2f, new Vector3(105, 0, 0))
            {
                OrbitCenter = _sun.Position,
                OrbitRadius = 105,
                OrbitSpeed = 3f,
                SelfRotate = 12f,
                AtmosphereTint = new Vector3(0.6f, 0.9f, 0.95f)
            };

            var neptune = new Planet("Neptune", neptuneTex, 3.0f, new Vector3(115, 0, 0))
            {
                OrbitCenter = _sun.Position,
                OrbitRadius = 115,
                OrbitSpeed = 2f,
                SelfRotate = 12f,
                AtmosphereTint = new Vector3(0.3f, 0.55f, 1.0f)
            };

            _planets.AddRange(new[] { sun, venus, earth, moon, mars, jupiter, saturn, uranus, neptune });

            // Rings: texture + flat ring mesh. The model matrix handles tilt and position.
            _saturnRingTex = Texture.Load2D(Path.Combine("Images", "saturn_rings.png"));
            _saturnRingMesh = Mesh.CreateRing(inner: saturn.Radius + 0.8f, outer: saturn.Radius + 4.4f);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            if (!IsFocused) return;

            float dt = (float)args.Time;
            var kb = KeyboardState;

            // Lock/unlock mouse and toggle light
            if (kb.IsKeyPressed(Keys.Escape))
            {
                _cursorLocked = !_cursorLocked;
                CursorState = _cursorLocked ? CursorState.Grabbed : CursorState.Normal;
            }

            if (kb.IsKeyPressed(Keys.E))
            {
                _lightOn = !_lightOn;
                _lightIntensity = _lightOn ? 3.5f : 0f;
            }

            // Move or drag
            if (_dragging == null)
            {
                _cam.UpdateKeyboard(kb, dt);
                var cur = MousePosition;
                if (_cursorLocked)
                {
                    var delta = cur - _lastMouse;
                    _cam.UpdateMouse(delta);
                }
                _lastMouse = cur;
            }
            else
            {
                // While dragging, ignore mouse deltas for camera rotation
                _lastMouse = MousePosition;
            }

            // Update planet orbits when not dragging
            foreach (var p in _planets)
            {
                if (_dragging != p)
                {
                    if (p != _sun)
                        p.OrbitCenter = (p == _moon) ? _earth.Position : _sun.Position;

                    p.Update(dt);
                    KeepInsideBox(p);
                }
            }

            HandleDragging(dt);
        }

        // Core dragging logic — turns 2D mouse motion into 3D planet movement.
        void HandleDragging(float dt)
        {
            var pickMouse = _cursorLocked ? new Vector2(Size.X * 0.5f, Size.Y * 0.5f) : MouseState.Position;
            var proj = _cam.Projection((float)Size.X / Size.Y);
            var view = _cam.View;
            var (ro, rd) = ScreenRay(pickMouse, view, proj, Size);

            // Helper for ray-plane intersection (used to find where the mouse "hits" in 3D space)
            static bool RayPlaneIntersect(in Vector3 ro, in Vector3 rd, in Vector3 n, float d, out Vector3 hit)
            {
                float denom = Vector3.Dot(n, rd);
                if (MathF.Abs(denom) < 1e-6f) { hit = Vector3.Zero; return false; }
                float t = (d - Vector3.Dot(n, ro)) / denom;
                if (t <= 0f) { hit = Vector3.Zero; return false; }
                hit = ro + rd * t;
                return true;
            }

            // --- Pick object ---
            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                Planet best = null;
                float bestT = float.MaxValue;

                foreach (var p in _planets)
                    if (p.RayHit(ro, rd, out float t) && t < bestT)
                    { bestT = t; best = p; }

                if (best != null)
                {
                    if (_dragging != null) _dragging.IsDragging = false;
                    _dragging = best;
                    _dragging.IsDragging = true;

                    // Define drag plane at the hit point, facing camera
                    Vector3 hitPos = ro + rd * bestT;
                    _dragOffset = hitPos - best.Position;
                    _dragPlaneN = Vector3.Normalize(_cam.Forward);
                    _dragPlaneD = Vector3.Dot(_dragPlaneN, hitPos);

                    _dragSavedOrbitSpeed = _dragging.OrbitSpeed;
                    _dragging.OrbitSpeed = 0f;
                    _dragStartMouse = pickMouse;
                    _dragPlaneHitStart = hitPos;

                    // Calculate how much a single pixel moves in world space (X/Y)
                    var (roR, rdR) = ScreenRay(pickMouse + new Vector2(1f, 0f), view, proj, Size);
                    RayPlaneIntersect(ro, rd, _dragPlaneN, _dragPlaneD, out var centerHit);
                    RayPlaneIntersect(roR, rdR, _dragPlaneN, _dragPlaneD, out var rightHit);
                    _dragPerPixelX = rightHit - centerHit;

                    var (roD, rdD) = ScreenRay(pickMouse + new Vector2(0f, 1f), view, proj, Size);
                    RayPlaneIntersect(roD, rdD, _dragPlaneN, _dragPlaneD, out var downHit);
                    _dragPerPixelY = downHit - centerHit;

                    _lastMouse = MousePosition;
                }
            }
            else if (MouseState.IsButtonReleased(MouseButton.Left) && _dragging != null)
            {
                _dragging.IsDragging = false;
                _dragging.OrbitSpeed = _dragSavedOrbitSpeed;
                _dragging = null;
            }

            // --- Update dragging ---
            if (_dragging != null && MouseState.IsButtonDown(MouseButton.Left))
            {
                var currentPickMouse = _cursorLocked ? new Vector2(Size.X * 0.5f, Size.Y * 0.5f) : MouseState.Position;
                Vector2 mouseDelta = currentPickMouse - _dragStartMouse;

                Vector3 planeHitNew = _dragPlaneHitStart + _dragPerPixelX * mouseDelta.X + _dragPerPixelY * mouseDelta.Y;
                Vector3 desired = planeHitNew - _dragOffset;

                // Smooth the motion so the planet eases into the target spot (no jitter).
                float alpha = 1f - MathF.Exp(-_dragSmoothK * MathF.Max(dt, 1e-4f));
                _dragging.Position = Vector3.Lerp(_dragging.Position, desired, alpha);
            }
        }

        // Converts 2D mouse position to a 3D world-space ray.
        (Vector3 ro, Vector3 rd) ScreenRay(Vector2 mouse, Matrix4 view, Matrix4 proj, Vector2i vp)
        {
            float x = (2f * mouse.X) / vp.X - 1f;
            float y = 1f - (2f * mouse.Y) / vp.Y;

            Matrix4 invVP = Matrix4.Invert(proj * view);
            Vector4 nearClip = new Vector4(x, y, -1f, 1f);
            Vector4 farClip = new Vector4(x, y, 1f, 1f);

            Vector4 nearW = invVP * nearClip;
            Vector4 farW = invVP * farClip;
            nearW /= nearW.W;
            farW /= farW.W;

            Vector3 origin = nearW.Xyz;
            Vector3 dir = Vector3.Normalize(farW.Xyz - nearW.Xyz);
            return (origin, dir);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var proj = _cam.Projection((float)Size.X / Size.Y);
            var view = _cam.View;
            _shader.Use();

            // Set base uniforms for rendering
            GL.UniformMatrix4(_shader.GetUniformLocation("uProj"), false, ref proj);
            GL.Uniform3(_shader.GetUniformLocation("uViewPos"), _cam.Position);

            // --- Draw Skybox ---
            var viewForSky = view; viewForSky.M41 = viewForSky.M42 = viewForSky.M43 = 0f;
            GL.UniformMatrix4(_shader.GetUniformLocation("uView"), false, ref viewForSky);
            GL.DepthMask(false);
            GL.Disable(EnableCap.CullFace);
            _skyTex.Bind(TextureUnit.Texture0);
            var skyModel = Matrix4.CreateScale(-BoxHalfSize);
            GL.UniformMatrix4(_shader.GetUniformLocation("uModel"), false, ref skyModel);
            GL.Uniform1(_shader.GetUniformLocation("uIsSky"), 1);
            _skyCube.Draw();
            GL.Uniform1(_shader.GetUniformLocation("uIsSky"), 0);
            GL.Enable(EnableCap.CullFace);
            GL.DepthMask(true);

            // --- Light + planet draw ---
            GL.UniformMatrix4(_shader.GetUniformLocation("uView"), false, ref view);
            var sunPos = _planets[0].Position;
            GL.Uniform3(_shader.GetUniformLocation("uLightPos"), sunPos);
            GL.Uniform3(_shader.GetUniformLocation("uLightColor"), new Vector3(1.2f, 1.1f, 1.0f));
            GL.Uniform1(_shader.GetUniformLocation("uLightIntensity"), _lightIntensity);

            // Find hovered planet (for subtle highlight)
            Vector2 pickMouse = _cursorLocked ? new Vector2(Size.X * 0.5f, Size.Y * 0.5f) : MouseState.Position;
            var (roHover, rdHover) = ScreenRay(pickMouse, view, proj, Size);
            Planet hoveredPlanet = null;
            float bestT = float.MaxValue;
            foreach (var p in _planets)
                if (p.RayHit(roHover, rdHover, out float t) && t < bestT)
                { bestT = t; hoveredPlanet = p; }

            foreach (var p in _planets)
            {
                p.IsHovered = (p == hoveredPlanet);
                p.IsDragging = (p == _dragging);
                p.Draw(_shader);
            }

            // Draw Saturn’s rings (simple unlit alpha)
            var saturn = _planets.Find(p => p.Name == "Saturn");
            if (saturn != null && _saturnRingMesh != null && _saturnRingTex != null)
            {
                var ringModel = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(26.7f)) *
                                Matrix4.CreateTranslation(saturn.Position);
                GL.UniformMatrix4(_shader.GetUniformLocation("uModel"), false, ref ringModel);
                GL.Uniform1(_shader.GetUniformLocation("uUnlit"), 1);
                _saturnRingTex.Bind(TextureUnit.Texture0);
                GL.Disable(EnableCap.CullFace);
                _saturnRingMesh.Draw();
                GL.Enable(EnableCap.CullFace);
                GL.Uniform1(_shader.GetUniformLocation("uUnlit"), 0);
            }

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            // Clean up GPU resources
            _shader?.Dispose();
            _skyCube?.Dispose();
            _saturnRingMesh?.Dispose();
            foreach (var p in _planets) p.Mesh?.Dispose();
        }

        // Keeps planets inside the visible skybox space (so nothing wanders off forever).
        void KeepInsideBox(Planet p)
        {
            float r = p.Radius;
            p.Position.X = MathHelper.Clamp(p.Position.X, -BoxHalfSize + r, BoxHalfSize - r);
            p.Position.Y = MathHelper.Clamp(p.Position.Y, -BoxHalfSize + r, BoxHalfSize - r);
            p.Position.Z = MathHelper.Clamp(p.Position.Z, -BoxHalfSize + r, BoxHalfSize - r);
        }
    }

    // Entry point: creates and runs the game window.
    public static class Program
    {
        public static void Main()
        {
            using var g = new Game();
            g.Run();
        }
    }
}
