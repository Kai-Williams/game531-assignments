using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace midtermGame.Graphics
{
    // Simple first-person-style camera.
    // Handles position, rotation, and projection math for 3D movement and viewing.
    public sealed class Camera
    {
        public Vector3 Position;          // where the camera is in 3D space
        public float Pitch, Yaw = -90f;   // rotation angles (up/down, left/right)
        public float Fov = 60f;           // field of view for perspective projection

        public Camera(Vector3 pos)
        {
            Position = pos;
        }

        // Returns a view matrix that represents the camera's position and orientation.
        public Matrix4 View =>
            Matrix4.LookAt(Position, Position + Forward, Up);

        // Returns a projection matrix (used to give the world perspective).
        public Matrix4 Projection(float aspect) =>
            Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(Fov), aspect, 0.1f, 1000f);

        // Forward direction vector — where the camera is looking.
        public Vector3 Forward => Vector3.Normalize(new Vector3(
            MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch)),
            MathF.Sin(MathHelper.DegreesToRadians(Pitch)),
            MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch))));

        // Right and Up vectors based on current orientation.
        public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
        public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

        // Moves the camera based on keyboard input (WASD + Space/Ctrl).
        public void UpdateKeyboard(KeyboardState kbd, float dt, float speed = 20f)
        {
            float v = speed * dt; // move amount based on frame time

            if (kbd.IsKeyDown(Keys.W)) Position += Forward * v;          // forward
            if (kbd.IsKeyDown(Keys.S)) Position -= Forward * v;          // backward
            if (kbd.IsKeyDown(Keys.A)) Position -= Right * v;            // strafe left
            if (kbd.IsKeyDown(Keys.D)) Position += Right * v;            // strafe right
            if (kbd.IsKeyDown(Keys.Space)) Position += Vector3.UnitY * v; // move up
            if (kbd.IsKeyDown(Keys.LeftControl)) Position -= Vector3.UnitY * v; // move down
        }

        // Rotates the camera based on mouse movement.
        public void UpdateMouse(Vector2 delta, float sens = 0.15f)
        {
            Yaw += delta.X * sens;
            Pitch -= delta.Y * sens;

            // Prevent flipping over when looking too far up/down.
            Pitch = MathHelper.Clamp(Pitch, -89f, 89f);
        }
    }
}
