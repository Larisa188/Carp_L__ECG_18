using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Camera orbita (TEMA): se roteste in jurul unei tinte (target) pe o sfera de raza egala cu distanta.
/// </summary>
public class OrbitCamera : Camera
{
    public Vector3 Target { get; set; } = new(0.0f, 1.0f, 0.0f);

    private float _distance = 8.0f;
    public float Distance
    {
        get => _distance;
        set => _distance = Math.Clamp(value, MinDistance, MaxDistance);
    }

    public float Azimuth { get; private set; } = 45.0f;
    public float Elevation { get; private set; } = 30.0f;

    public const float MinDistance = 2.0f;
    public const float MaxDistance = 30.0f;
    public const float MaxElevation = 89.0f;
    public float MouseSensitivity = 0.2f;
    public float KeyRotationSpeed = 90.0f;
    public float ZoomStep = 0.8f;

    public OrbitCamera() => Reset();

    public override string Name => "Orbita";

    public override Vector3 Position
    {
        get
        {
            float elRad = MathHelper.DegreesToRadians(Elevation);
            float azRad = MathHelper.DegreesToRadians(Azimuth);
            return Target + _distance * new Vector3(
                MathF.Cos(elRad) * MathF.Cos(azRad),
                MathF.Sin(elRad),
                MathF.Cos(elRad) * MathF.Sin(azRad));
        }
    }

    public override Vector3 Front => Vector3.Normalize(Target - Position);
    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth -= mouseDelta.X * MouseSensitivity;
            Elevation = Math.Clamp(Elevation + mouseDelta.Y * MouseSensitivity, -MaxElevation, MaxElevation);
        }

        if (keyboard.IsKeyDown(Keys.Left)) Azimuth += KeyRotationSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth -= KeyRotationSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation = Math.Clamp(Elevation + KeyRotationSpeed * dt, -MaxElevation, MaxElevation);
        if (keyboard.IsKeyDown(Keys.Down)) Elevation = Math.Clamp(Elevation - KeyRotationSpeed * dt, -MaxElevation, MaxElevation);

        if (mouse.ScrollDelta.Y != 0)
            Distance -= mouse.ScrollDelta.Y * ZoomStep;
    }

    public override void Reset()
    {
        Target = new Vector3(0.0f, 1.0f, 0.0f);
        Distance = 8.0f;
        Azimuth = 45.0f;
        Elevation = 30.0f;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Tinta          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanta       : {Distance,6:F2}";
        yield return $"Azimut / elev. : {Azimuth,7:F1}° / {Elevation,6:F1}°";
        yield return $"Pozitie        : ({Position.X,6:F2}, {Position.Y,6:F2}, {Position.Z,6:F2})";
    }
}
