using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public sealed class RobotArmScene : Scene
{
    private const string ActBaseLeft = "BaseLeft";
    private const string ActBaseRight = "BaseRight";
    private const string ActShoulderUp = "ShoulderUp";
    private const string ActShoulderDown = "ShoulderDown";
    private const string ActElbowUp = "ElbowUp";
    private const string ActElbowDown = "ElbowDown";
    private const string ActGripper = "Gripper";
    private const string ActSavePose = "SavePose";
    private const string ActPlayLoop = "PlayLoop";
    private const string ActCycleEase = "CycleEase";
    private const string ActClearPoses = "ClearPoses";

    private const float BaseSpeed = 90.0f;
    private const float ShoulderSpeed = 70.0f;
    private const float ElbowSpeed = 80.0f;

    private const float MinShoulder = -60.0f;
    private const float MaxShoulder = 75.0f;
    private const float MinElbow = -110.0f;
    private const float MaxElbow = 110.0f;

    private SceneObject _basePivot = null!;
    private SceneObject _shoulderPivot = null!;
    private SceneObject _elbowPivot = null!;
    private SceneObject _leftJawPivot = null!;
    private SceneObject _rightJawPivot = null!;

    private float _baseYaw;
    private float _shoulderPitch;
    private float _elbowPitch;

    private bool _gripperOpen;
    private Tween? _gripperTween;
    private float _gripperAngle;

    private readonly List<Vector3> _savedPoses = new();
    private KeyframeTrack? _track;
    private bool _playing;
    private float _playTime;
    private int _easeIndex = 3; // EaseInOut

    public override string Name => "Temă A - Braț robotic";

    public override string Status
    {
        get
        {
            string mode = _playing ? "REDARE" : "MANUAL";
            string grip = _gripperOpen ? "deschis" : "închis";
            string easeName = Easing.All[_easeIndex].Name;
            return $"{mode} | clește: {grip} | poze: {_savedPoses.Count} | ease: {easeName}";
        }
    }

    public override IEnumerable<string> ActionNames => new[]
    {
        ActBaseLeft,ActBaseRight, ActShoulderUp,ActShoulderDown, ActElbowUp, ActElbowDown, ActGripper,
        ActSavePose, ActPlayLoop, ActCycleEase, ActClearPoses,
    };

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 5.5f, 11.0f);
        CameraTarget = new Vector3(0.0f, 2.2f, 0.0f);

        input.Bind(ActBaseLeft, Keys.A);
        input.Bind(ActBaseRight, Keys.D);
        input.Bind(ActShoulderUp, Keys.W);
        input.Bind(ActShoulderDown, Keys.S);
        input.Bind(ActElbowUp, Keys.Up);
        input.Bind(ActElbowDown, Keys.Down);
        input.Bind(ActGripper, Keys.Space);
        input.Bind(ActSavePose, Keys.K);
        input.Bind(ActPlayLoop, Keys.Enter);
        input.Bind(ActCycleEase, Keys.E);
        input.Bind(ActClearPoses, Keys.Backspace);

        Mesh cube = AddMesh(Shapes.Cube());

        var pedestal = AddObject("Pedestal", cube);
        pedestal.Transform.Position = new Vector3(0.0f, 0.25f, 0.0f);
        pedestal.Transform.Scale = new Vector3(2.2f, 0.5f, 2.2f);
        pedestal.Tint = new Color4(0.35f, 0.38f, 0.44f, 1.0f);

        _basePivot = AddObject("BasePivot", null, pedestal);
        _basePivot.Transform.Position = new Vector3(0.0f, 0.25f, 0.0f);

        var turret = AddObject("Turret", cube, _basePivot);
        turret.Transform.Position = new Vector3(0.0f, 0.35f, 0.0f);
        turret.Transform.Scale = new Vector3(1.2f, 0.7f, 1.2f);
        turret.Tint = new Color4(0.95f, 0.55f, 0.15f, 1.0f);

        _shoulderPivot = AddObject("ShoulderPivot", null, _basePivot);
        _shoulderPivot.Transform.Position = new Vector3(0.0f, 0.7f, 0.0f);

        float upperLen = 2.2f;
        var upperArm = AddObject("UpperArm", cube, _shoulderPivot);
        upperArm.Transform.Position = new Vector3(0.0f, upperLen * 0.5f, 0.0f);
        upperArm.Transform.Scale = new Vector3(0.42f, upperLen, 0.42f);
        upperArm.Tint = new Color4(0.82f, 0.85f, 0.90f, 1.0f);

        _elbowPivot = AddObject("ElbowPivot", null, _shoulderPivot);
        _elbowPivot.Transform.Position = new Vector3(0.0f, upperLen, 0.0f);

        float foreLen = 1.9f;
        var forearm = AddObject("Forearm", cube, _elbowPivot);
        forearm.Transform.Position = new Vector3(0.0f, foreLen * 0.5f, 0.0f);
        forearm.Transform.Scale = new Vector3(0.34f, foreLen, 0.34f);
        forearm.Tint = new Color4(0.25f, 0.65f, 0.95f, 1.0f);

        var wrist = AddObject("Wrist", cube, _elbowPivot);
        wrist.Transform.Position = new Vector3(0.0f, foreLen + 0.15f, 0.0f);
        wrist.Transform.Scale = new Vector3(0.9f, 0.3f, 0.4f);
        wrist.Tint = new Color4(0.45f, 0.48f, 0.55f, 1.0f);

        _leftJawPivot = AddObject("LeftJawPivot", null, wrist);
        _leftJawPivot.Transform.Position = new Vector3(-0.35f, 0.15f, 0.0f);
        var leftJaw = AddObject("LeftJaw", cube, _leftJawPivot);
        leftJaw.Transform.Position = new Vector3(0.0f, 0.4f, 0.0f);
        leftJaw.Transform.Scale = new Vector3(0.16f, 0.8f, 0.25f);
        leftJaw.Tint = new Color4(0.95f, 0.35f, 0.35f, 1.0f);

        _rightJawPivot = AddObject("RightJawPivot", null, wrist);
        _rightJawPivot.Transform.Position = new Vector3(0.35f, 0.15f, 0.0f);
        var rightJaw = AddObject("RightJaw", cube, _rightJawPivot);
        rightJaw.Transform.Position = new Vector3(0.0f, 0.4f, 0.0f);
        rightJaw.Transform.Scale = new Vector3(0.16f, 0.8f, 0.25f);
        rightJaw.Tint = new Color4(0.95f, 0.35f, 0.35f, 1.0f);

        ApplyTransforms();
    }

    public override void Update(float dt, Input input)
    {
        if (input.WasPressed(ActCycleEase))
        {
            _easeIndex = (_easeIndex + 1) % Easing.All.Length;
            if (_track != null)
            {
                _track.Ease = Easing.All[_easeIndex].Function;
            }
            ConsolePanel.PrintMessage($"Easing redare: {Easing.All[_easeIndex].Name}");
        }

        if (input.WasPressed(ActSavePose))
        {
            _savedPoses.Add(new Vector3(_baseYaw, _shoulderPitch, _elbowPitch));
            ConsolePanel.PrintMessage($"Poză salvată (#{_savedPoses.Count}): ({_baseYaw:F1}°, {_shoulderPitch:F1}°, {_elbowPitch:F1}°)");
        }

        if (input.WasPressed(ActClearPoses))
        {
            _savedPoses.Clear();
            _track = null;
            _playing = false;
            _playTime = 0.0f;
            ConsolePanel.PrintMessage("Pozele salvate au fost șterse.");
        }

        if (input.WasPressed(ActPlayLoop))
        {
            if (_savedPoses.Count >= 2)
            {
                _playing = !_playing;
                _playTime = 0.0f;
                if (_playing)
                {
                    _track = new KeyframeTrack(1.0f)
                    {
                        Ease = Easing.All[_easeIndex].Function
                    };
                    foreach (Vector3 pose in _savedPoses)
                    {
                        _track.Add(pose);
                    }
                }
                ConsolePanel.PrintMessage(_playing ? "Redare poze în buclă: PORNITĂ" : "Redare poze în buclă: OPRITĂ");
            }
            else
            {
                ConsolePanel.PrintMessage("Salvează cel puțin 2 poze (tasta K) pentru redare.");
            }
        }

        if (input.WasPressed(ActGripper))
        {
            _gripperOpen = !_gripperOpen;
            float targetAngle = _gripperOpen ? 28.0f : 0.0f;
            _gripperTween = new Tween(
                new Vector3(_gripperAngle, 0.0f, 0.0f),
                new Vector3(targetAngle, 0.0f, 0.0f),
                0.35f,
                Easing.EaseOut);
        }

        if (_gripperTween != null)
        {
            _gripperTween.Update(dt);
            _gripperAngle = _gripperTween.Value.X;
            if (_gripperTween.IsFinished)
            {
                _gripperTween = null;
            }
        }

        if (_playing && _track != null)
        {
            _playTime += dt;
            Vector3 sampled = _track.Sample(_playTime);
            _baseYaw = sampled.X;
            _shoulderPitch = Math.Clamp(sampled.Y, MinShoulder, MaxShoulder);
            _elbowPitch = Math.Clamp(sampled.Z, MinElbow, MaxElbow);
        }
        else
        {
            float baseDir = (input.IsDown(ActBaseRight) ? 1.0f : 0.0f) - (input.IsDown(ActBaseLeft) ? 1.0f : 0.0f);
            float shoulderDir = (input.IsDown(ActShoulderUp) ? 1.0f : 0.0f) - (input.IsDown(ActShoulderDown) ? 1.0f : 0.0f);
            float elbowDir = (input.IsDown(ActElbowUp) ? 1.0f : 0.0f) - (input.IsDown(ActElbowDown) ? 1.0f : 0.0f);

            _baseYaw += baseDir * BaseSpeed * dt;
            _shoulderPitch = Math.Clamp(_shoulderPitch + shoulderDir * ShoulderSpeed * dt, MinShoulder, MaxShoulder);
            _elbowPitch = Math.Clamp(_elbowPitch + elbowDir * ElbowSpeed * dt, MinElbow, MaxElbow);
        }

        ApplyTransforms();
        base.Update(dt, input);
    }

    private void ApplyTransforms()
    {
        _basePivot.Transform.Rotation = new Vector3(0.0f, _baseYaw, 0.0f);
        _shoulderPivot.Transform.Rotation = new Vector3(_shoulderPitch, 0.0f, 0.0f);
        _elbowPivot.Transform.Rotation = new Vector3(_elbowPitch, 0.0f, 0.0f);

        _leftJawPivot.Transform.Rotation = new Vector3(0.0f, 0.0f, _gripperAngle);
        _rightJawPivot.Transform.Rotation = new Vector3(0.0f, 0.0f, -_gripperAngle);
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Bază (Y)     : {_baseYaw:F1}°";
        yield return $"Umăr (X)      : {_shoulderPitch:F1}° [{MinShoulder}..{MaxShoulder}]";
        yield return $"Cot (X)       : {_elbowPitch:F1}° [{MinElbow}..{MaxElbow}]";
        yield return $"Clește        : {(_gripperOpen ? "deschis" : "închis")} ({_gripperAngle:F1}°)";
        yield return $"Poze / Easing : {_savedPoses.Count} poze / {Easing.All[_easeIndex].Name}";
    }
}
