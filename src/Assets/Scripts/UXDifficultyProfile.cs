public enum UXDifficulty { Easy, Hard }

// Immutable profiles keep visible vision cones and the actual detection rules in sync.
public sealed class UXDifficultyProfile
{
    public readonly UXDifficulty Mode;
    public readonly float PatrolSpeed, ResponseSpeed, GuardRange, GuardAngle;
    public readonly float CameraRange, CameraAngle, CameraSweepSpeed, CameraSweepArc, AlertGrowth;
    public float AimTime => Mode == UXDifficulty.Hard ? 0.45f : 1f;
    public float ShotCooldown => Mode == UXDifficulty.Hard ? 0.90f : 1.6f;
    public float ShootingRange => Mode == UXDifficulty.Hard ? 4.4f : 3.6f;
    public int HitDamage => Mode == UXDifficulty.Hard ? 34 : 25;
    public float InteractionDuration => Mode == UXDifficulty.Hard ? 2f : 1f;
    public float ExitDoorDuration => 8f;
    public float SearchPatience => Mode == UXDifficulty.Hard ? 1.5f : 1f;

    private UXDifficultyProfile(UXDifficulty mode, float patrol, float response,
        float guardRange, float guardAngle, float cameraRange, float cameraAngle,
        float sweepSpeed, float sweepArc, float alertGrowth)
    {
        Mode = mode; PatrolSpeed = patrol; ResponseSpeed = response;
        GuardRange = guardRange; GuardAngle = guardAngle;
        CameraRange = cameraRange; CameraAngle = cameraAngle;
        CameraSweepSpeed = sweepSpeed; CameraSweepArc = sweepArc; AlertGrowth = alertGrowth;
    }

    private static readonly UXDifficultyProfile Easy = new UXDifficultyProfile(
        UXDifficulty.Easy, 0.85f, 0.90f, 0.95f, 0.85f, 0.95f, 0.90f, 0.85f, 0.90f, 0.75f);
    private static readonly UXDifficultyProfile Hard = new UXDifficultyProfile(
        UXDifficulty.Hard, 1.55f, 1.50f, 1.30f, 1.45f, 1.15f, 1.50f, 1.30f, 1.40f, 1.80f);

    public static UXDifficultyProfile For(UXDifficulty mode) => mode == UXDifficulty.Hard ? Hard : Easy;
}
