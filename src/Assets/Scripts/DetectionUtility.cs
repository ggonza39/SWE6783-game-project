using UnityEngine;

/// <summary>
/// Shared field-of-view and line-of-sight math used by both GuardDetection and
/// SecurityCamera so the two systems can never drift out of sync with each other
/// or with their visible vision-cone gizmos.
/// </summary>
public static class DetectionUtility
{
    /// <summary>
    /// Returns true when <paramref name="targetPosition"/> lies within range and
    /// inside the field-of-view cone defined by <paramref name="forward"/> and
    /// <paramref name="fovAngle"/> (the cone's full angle, not the half-angle).
    /// </summary>
    public static bool IsWithinFieldOfView(
        Vector2 origin,
        Vector2 forward,
        Vector2 targetPosition,
        float range,
        float fovAngle,
        out float distance,
        out float angle)
    {
        Vector2 toTarget = targetPosition - origin;
        distance = toTarget.magnitude;

        // A zero-length forward vector has no meaningful facing direction.
        angle = forward.sqrMagnitude > 0f
            ? Vector2.Angle(forward, toTarget)
            : 180f;

        return distance <= range && angle <= fovAngle / 2f;
    }

    /// <summary>
    /// Casts a ray from <paramref name="origin"/> toward <paramref name="targetPosition"/>
    /// and returns true only if no solid (non-trigger) collider blocks the path,
    /// ignoring the detector's own collider so guards/cameras don't self-obstruct.
    /// </summary>
    public static bool HasLineOfSight(
        Vector2 origin,
        Vector2 targetPosition,
        Transform self,
        RaycastHit2D[] hitBuffer)
    {
        Vector2 offset = targetPosition - origin;
        float distance = offset.magnitude;

        if (distance <= Mathf.Epsilon)
            return true;

        Vector2 direction = offset / distance;
        int hitCount = Physics2D.RaycastNonAlloc(origin, direction, hitBuffer, distance);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i].collider;

            if (hitCollider == null)
                continue;

            // Ignore the detector's own collider (raycasts can register a hit
            // at the origin when it starts inside its own collider bounds).
            if (self != null && hitCollider.transform == self)
                continue;

            // Triggers (hiding spots, evidence, terminals, extraction zone)
            // never block line of sight; only solid geometry (walls) does.
            if (hitCollider.isTrigger)
                continue;

            return false;
        }

        return true;
    }
}
