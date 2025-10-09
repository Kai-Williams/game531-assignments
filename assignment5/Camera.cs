using OpenTK.Mathematics;

namespace PhongOpenTK
{
    public class Camera
    {
        public Vector3 Position;
        public float Pitch;
        public float Yaw;  
        public float Aspect;

        public float Fov = MathHelper.DegreesToRadians(60f);
        public float Near = 0.1f;
        public float Far = 100f;

        public Camera(Vector3 pos, float aspect)
        {
            Position = pos;
            Aspect = aspect;
            Yaw = -90f; 
            Pitch = 0f;
        }

        public Vector3 Front
        {
            get
            {
                var front = new Vector3(
                    MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch)),
                    MathF.Sin(MathHelper.DegreesToRadians(Pitch)),
                    MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch))
                );
                return Vector3.Normalize(front);
            }
        }

        public Vector3 Right => Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Front));

        public Matrix4 GetViewMatrix() => Matrix4.LookAt(Position, Position + Front, Up);
        public Matrix4 GetProjectionMatrix() => Matrix4.CreatePerspectiveFieldOfView(Fov, Aspect, Near, Far);
    }
}
