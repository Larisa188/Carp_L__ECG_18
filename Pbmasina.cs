using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public sealed class Pbmasina : Scene
{
    private const string ActForward = "Forward";
    private const string ActBackward = "Backward";
    private const string ActLeft = "Left";
    private const string ActRight = "Right";
    private const string ActToggleBounce = "ToggleBounce";
    private const string ActCycleEase = "CycleEase";

    private const float MoveSpeed = 6.0f;
    private const float TurnSpeed = 110.0f;
    private const float BallSpanX = 3.2f;
    private const float BallBaseY = 2.1f;
    private const float BallJumpHeight = 1.6f;

    private SceneObject _carRoot = null!;
    private SceneObject _ball = null!;

    private float _yaw;
    private bool _bouncing = true;
    private bool _goingRight = true;
    private Tween? _xTween;
    private float _bouncePhase;
    private int _easeIndex = 3;

    public override string Name => "Temă - Mașină și minge";

    public override string Status
    {
        get
        {
            string state = _bouncing ? "MINGE ACTIVĂ" : "MINGE OPRITĂ";
            string easeName = Easing.All[_easeIndex].Name;
            return $"{state} | ease: {easeName} | pos: ({_carRoot.Transform.Position.X:F1}, {_carRoot.Transform.Position.Z:F1})";
        }
    }

    public override IEnumerable<string> ActionNames => new[]
    {
        ActForward, ActBackward, ActLeft, ActRight, ActToggleBounce, ActCycleEase,
    };

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 7.5f, 13.5f);
        CameraTarget = new Vector3(0.0f, 1.0f, 0.0f);

        input.Bind(ActForward, Keys.W);
        input.Bind(ActBackward, Keys.S);
        input.Bind(ActLeft, Keys.A);
        input.Bind(ActRight, Keys.D);
        input.Bind(ActToggleBounce, Keys.Space);
        input.Bind(ActCycleEase, Keys.E);

        Mesh cube = AddMesh(Shapes.Cube());

        _carRoot = AddObject("CarRoot", null);
        _carRoot.Transform.Position = new Vector3(0.0f, 0.55f, 0.0f);

        var body = AddObject("CarBody", cube, _carRoot);
        body.Transform.Position = new Vector3(0.0f, 0.24f, 0.0f);
        body.Transform.Scale = new Vector3(1.85f, 0.55f, 3.3f);
        body.Tint = new Color4(0.18f, 0.52f, 0.88f, 1.0f);

        var hoodAccent = AddObject("CarHood", cube, _carRoot);
        hoodAccent.Transform.Position = new Vector3(0.0f, 0.53f, 0.85f);
        hoodAccent.Transform.Scale = new Vector3(1.55f, 0.08f, 1.15f);
        hoodAccent.Tint = new Color4(0.32f, 0.68f, 0.98f, 1.0f);

        var cabin = AddObject("CarCabin", cube, _carRoot);
        cabin.Transform.Position = new Vector3(0.0f, 0.78f, -0.25f);
        cabin.Transform.Scale = new Vector3(1.35f, 0.48f, 1.55f);
        cabin.Tint = new Color4(0.82f, 0.93f, 1.0f, 1.0f);

        var headLightL = AddObject("HeadLightL", cube, _carRoot);
        headLightL.Transform.Position = new Vector3(-0.62f, 0.28f, 1.68f);
        headLightL.Transform.Scale = new Vector3(0.32f, 0.2f, 0.1f);
        headLightL.Tint = new Color4(1.0f, 0.92f, 0.45f, 1.0f);

        var headLightR = AddObject("HeadLightR", cube, _carRoot);
        headLightR.Transform.Position = new Vector3(0.62f, 0.28f, 1.68f);
        headLightR.Transform.Scale = new Vector3(0.32f, 0.2f, 0.1f);
        headLightR.Tint = new Color4(1.0f, 0.92f, 0.45f, 1.0f);

        var tailLightL = AddObject("TailLightL", cube, _carRoot);
        tailLightL.Transform.Position = new Vector3(-0.62f, 0.28f, -1.68f);
        tailLightL.Transform.Scale = new Vector3(0.32f, 0.2f, 0.1f);
        tailLightL.Tint = new Color4(0.95f, 0.22f, 0.25f, 1.0f);

        var tailLightR = AddObject("TailLightR", cube, _carRoot);
        tailLightR.Transform.Position = new Vector3(0.62f, 0.28f, -1.68f);
        tailLightR.Transform.Scale = new Vector3(0.32f, 0.2f, 0.1f);
        tailLightR.Tint = new Color4(0.95f, 0.22f, 0.25f, 1.0f);

        AddWheel("WheelFL", cube, _carRoot, new Vector3(-1.02f, -0.22f, 1.08f));
        AddWheel("WheelFR", cube, _carRoot, new Vector3(1.02f, -0.22f, 1.08f));
        AddWheel("WheelRL", cube, _carRoot, new Vector3(-1.02f, -0.22f, -1.08f));
        AddWheel("WheelRR", cube, _carRoot, new Vector3(1.02f, -0.22f, -1.08f));

        Mesh sphere = AddMesh(Shapes.Sphere(0.36f, 24, 16));

        _ball = AddObject("BallPivot", null);
        _ball.Spin = new Vector3(0.0f, 120.0f, 0.0f);

        var ballMesh = AddObject("BallMesh", sphere, _ball);
        ballMesh.Tint = new Color4(1.0f, 0.75f, 0.15f, 1.0f);
        StartNextBallTween(-BallSpanX, BallSpanX);
        UpdateBallPosition();
    }

    private void AddWheel(string name, Mesh mesh, SceneObject parent, Vector3 localPos)
    {
        var wheel = AddObject(name, mesh, parent);
        wheel.Transform.Position = localPos;
        wheel.Transform.Scale = new Vector3(0.32f, 0.65f, 0.65f);
        wheel.Tint = new Color4(0.18f, 0.20f, 0.24f, 1.0f);
    }

    private void StartNextBallTween(float fromX, float toX)
    {
        _xTween = new Tween(
            new Vector3(fromX, 0.0f, 0.0f),
            new Vector3(toX, 0.0f, 0.0f),
            1.1f,
            Easing.All[_easeIndex].Function);
        _bouncePhase = 0.0f;
    }

    public override void Update(float dt, Input input)
    {
        if (input.WasPressed(ActCycleEase))
        {
            _easeIndex = (_easeIndex + 1) % Easing.All.Length;
            ConsolePanel.PrintMessage($"Easing minge: {Easing.All[_easeIndex].Name}");
        }

        if (input.WasPressed(ActToggleBounce))
        {
            _bouncing = !_bouncing;
            ConsolePanel.PrintMessage(_bouncing ? "Săritura mingii: PORNITĂ" : "Săritura mingii: OPRITĂ");
        }

        float moveDir = (input.IsDown(ActForward) ? 1.0f : 0.0f) - (input.IsDown(ActBackward) ? 1.0f : 0.0f);
        float turnDir = (input.IsDown(ActLeft) ? 1.0f : 0.0f) - (input.IsDown(ActRight) ? 1.0f : 0.0f);

        _yaw += turnDir * TurnSpeed * dt;
        _carRoot.Transform.Rotation = new Vector3(0.0f, _yaw, 0.0f);

        float rad = MathHelper.DegreesToRadians(_yaw);
        Vector3 forward = new Vector3(MathF.Sin(rad), 0.0f, MathF.Cos(rad));
        _carRoot.Transform.Position += forward * (moveDir * MoveSpeed * dt);

        if (_bouncing)
        {
            if (_xTween == null)
            {
                float startX = _goingRight ? -BallSpanX : BallSpanX;
                float endX = _goingRight ? BallSpanX : -BallSpanX;
                StartNextBallTween(startX, endX);
            }

            _xTween.Update(dt);
            _bouncePhase += dt * MathF.PI;

            if (_xTween.IsFinished)
            {
                _goingRight = !_goingRight;
                _xTween = null;
            }
        }

        UpdateBallPosition();
        base.Update(dt, input);
    }

    private void UpdateBallPosition()
    {
        float ballX = _xTween != null ? _xTween.Value.X : (_goingRight ? -BallSpanX : BallSpanX);
        float jumpOffset = MathF.Abs(MathF.Sin(_bouncePhase)) * BallJumpHeight;
        Vector3 carPos = _carRoot.Transform.Position;

        _ball.Transform.Position = new Vector3(
            carPos.X + ballX,
            carPos.Y + BallBaseY + jumpOffset,
            carPos.Z);
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Poziție mașină : ({_carRoot.Transform.Position.X:F2}, {_carRoot.Transform.Position.Z:F2})";
        yield return $"Unghi mașină   : {_yaw:F1}°";
        yield return $"Săritură minge : {(_bouncing ? "activă" : "oprită")} ({Easing.All[_easeIndex].Name})";
    }
}