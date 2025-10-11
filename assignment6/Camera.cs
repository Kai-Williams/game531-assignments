using OpenTK.Mathematics;
using System;
public class Camera
{
    public Vector3 Position { get; private set; }
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public float Fov { get; private set; } = MathHelper.DegreesToRadians(60f);
    public float NearPlane { get; } = 0.1f;
    public float FarPlane { get; } = 100f;
    public float MouseSensitivity { get; set; } = 0.1f;
    public float MoveSpeed { get; set; } = 3.5f;


    private Vector3 _front = -Vector3.UnitZ;
    private Vector3 _up = Vector3.UnitY;
    private Vector3 _right = Vector3.UnitX;


    public Camera(Vector3 startPos, float yawDeg = -90f, float pitchDeg = 0f)
    {
        Position = startPos;
        Yaw = yawDeg;
        Pitch = pitchDeg;
        UpdateVectors();
    }


    // Example 2
    public void ProcessKeyboard(Vector2 moveAxis, float deltaTime)
    {
        var velocity = MoveSpeed * deltaTime;
        Position += _front * (moveAxis.Y * velocity);
        Position += _right * (moveAxis.X * velocity);
    }


    // Example 3
    public void ProcessMouseMove(float deltaX, float deltaY)
    {
        deltaX *= MouseSensitivity;
        deltaY *= MouseSensitivity;


        Yaw += deltaX;
        Pitch -= deltaY;
        Pitch = MathHelper.Clamp(Pitch, -89f, 89f);
        UpdateVectors();
    }


    // Example 4
    public void ProcessMouseScroll(float scrollDelta)
    {
        var deg = MathHelper.RadiansToDegrees(Fov);
        deg -= scrollDelta * 2.5f;
        deg = MathHelper.Clamp(deg, 30f, 90f);
        Fov = MathHelper.DegreesToRadians(deg);
    }

    public Matrix4 GetViewMatrix() => Matrix4.LookAt(Position, Position + _front, _up);
    public Matrix4 GetProjectionMatrix(float aspect) => Matrix4.CreatePerspectiveFieldOfView(Fov, aspect, NearPlane, FarPlane);


    private void UpdateVectors()
    {
        float yawRad = MathHelper.DegreesToRadians(Yaw);
        float pitchRad = MathHelper.DegreesToRadians(Pitch);


        var front = new Vector3(
        MathF.Cos(pitchRad) * MathF.Cos(yawRad),
        MathF.Sin(pitchRad),
        MathF.Cos(pitchRad) * MathF.Sin(yawRad)
        );


        _front = Vector3.Normalize(front);
        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }
}
