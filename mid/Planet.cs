using midtermGame.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace midtermGame
{
    // Each Planet handles its own texture, mesh, spin, orbit and hover/drag state.
    // Basically, this class makes the planet "come alive" visually and logically.
    public sealed class Planet
    {
        public string Name;
        public Vector3 Position;
        public float Radius;
        public Texture Texture;
        public Mesh Mesh;
        public Vector3 AtmosphereTint = Vector3.Zero;

        public float OrbitSpeed;
        public Vector3 OrbitCenter;
        public float OrbitRadius;
        public float SelfRotate;
        float _orbitAngle;
        float _selfAngle;

        public bool IsHovered { get; set; }
        public bool IsDragging { get; set; }

        public Planet(string name, Texture tex, float radius, Vector3 startPos)
        {
            Name = name;
            Texture = tex;
            Radius = radius;
            Position = startPos;

            // Creates a smooth UV sphere mesh based on radius.
            Mesh = Mesh.CreateUvSphere(48, 64, radius);
        }

        public void Update(float dt)
        {
            if (!IsDragging && OrbitSpeed != 0 && OrbitRadius > 0)
            {
                _orbitAngle += OrbitSpeed * dt;
                float r = MathHelper.DegreesToRadians(_orbitAngle);
                Position = OrbitCenter + new Vector3(MathF.Cos(r) * OrbitRadius, 0, MathF.Sin(r) * OrbitRadius);
            }

            // Handles planet’s own rotation (spinning on its axis).
            if (SelfRotate != 0f)
            {
                _selfAngle += SelfRotate * dt;
                if (_selfAngle > 360f) _selfAngle -= 360f;
            }
        }

        public void Draw(Shader shader)
        {
            Texture.Bind(TextureUnit.Texture0);

            // Rotates planet
            var model =
                Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_selfAngle)) *
                Matrix4.CreateTranslation(Position);

            int loc = shader.GetUniformLocation("uModel");
            if (loc != -1)
                GL.UniformMatrix4(loc, false, ref model);

            int tintLoc = shader.GetUniformLocation("uTint");
            if (tintLoc != -1)
            {
                var tint = AtmosphereTint;

                // Slight highlight when hovered or dragged.
                if (IsHovered) tint += new Vector3(0.22f);
                if (IsDragging) tint += new Vector3(0.35f);

                GL.Uniform3(tintLoc, tint);
            }

            int highlightLoc = shader.GetUniformLocation("uHighlight");
            if (highlightLoc != -1)
            {
                float h = 0f;
                if (IsHovered) h = 1f;
                if (IsDragging) h = MathF.Max(h, 1f);
                GL.Uniform1(highlightLoc, h);
            }

            // Actually draw the mesh.
            Mesh.Draw();

            // Reset tint and highlight after drawing.
            if (tintLoc != -1)
                GL.Uniform3(tintLoc, Vector3.Zero);
            if (highlightLoc != -1)
                GL.Uniform1(highlightLoc, 0f);
        }

        // Simple math to check if a ray (from the mouse) hits the planet’s sphere.
        public bool RayHit(Vector3 ro, Vector3 rd, out float tHit)
        {
            Vector3 oc = ro - Position;
            float b = Vector3.Dot(oc, rd);
            float c = Vector3.Dot(oc, oc) - Radius * Radius;
            float disc = b * b - c;
            if (disc < 0) { tHit = 0; return false; }

            float t = -b - MathF.Sqrt(disc);
            if (t < 0) t = -b + MathF.Sqrt(disc);
            tHit = t;
            return t >= 0;
        }

        // Finds a point directly on the surface facing the viewer.
        public Vector3 GetSurfacePoint(Vector3 fromPos)
        {
            var dir = Vector3.Normalize(Position - fromPos);
            return Position - dir * Radius;
        }
    }
}
