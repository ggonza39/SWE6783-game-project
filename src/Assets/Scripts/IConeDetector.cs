using UnityEngine;

/// <summary>
/// Minimal interface so a single visual renderer can draw either a guard's or
/// a camera's field-of-view cone using the exact same numbers the detection
/// logic uses, guaranteeing the visible cone can never drift from actual
/// detection behavior.
/// </summary>
public interface IConeDetector
{
    Vector2 ConeOrigin { get; }
    Vector2 ConeForward { get; }
    float ConeRange { get; }
    float ConeFovAngle { get; }
    Color ConeColor { get; }
    bool ConeActive { get; }
}
