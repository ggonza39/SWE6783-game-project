using System;

// The expanded demo's rules are independent of rendering and Unity frame order.
public enum UXAlertPhase { Clear, Suspicious, Engaged, Searching }

public sealed class UXStealthState
{
    public int Health { get; private set; } = 100;
    public float Alert { get; private set; }
    public UXAlertPhase Phase { get; private set; }
    public bool Responding => Phase == UXAlertPhase.Engaged || Phase == UXAlertPhase.Searching;
    public bool Alive => Health > 0;
    private float unseenTime;
    private float hitProtection;
    private readonly float alertGrowth;
    private readonly float searchPatience;

    public UXStealthState(float alertGrowthMultiplier = 1f, float searchPatienceMultiplier = 1f)
    {
        alertGrowth = float.IsNaN(alertGrowthMultiplier) || float.IsInfinity(alertGrowthMultiplier)
            ? 1f : Math.Max(0.5f, Math.Min(2.5f, alertGrowthMultiplier));
        searchPatience = float.IsNaN(searchPatienceMultiplier) || float.IsInfinity(searchPatienceMultiplier)
            ? 1f : Math.Max(1f, Math.Min(2f, searchPatienceMultiplier));
    }

    public void Tick(float seconds, bool guardSeesPlayer, bool cameraSeesPlayer, bool hidden)
    {
        if (seconds <= 0f || !Alive) return;
        hitProtection = Math.Max(0f, hitProtection - seconds);
        bool seen = !hidden && (guardSeesPlayer || cameraSeesPlayer);
        if (seen)
        {
            unseenTime = 0f;
            Alert = Math.Min(100f, Alert + seconds * (cameraSeesPlayer ? 125f : 85f) * alertGrowth);
            Phase = Alert >= 100f || Responding ? UXAlertPhase.Engaged : UXAlertPhase.Suspicious;
            return;
        }

        unseenTime += seconds;
        if (Phase == UXAlertPhase.Engaged && unseenTime < 1.6f * searchPatience) return;
        if (Responding)
        {
            Phase = UXAlertPhase.Searching;
            // Time is measured since line of sight broke, not since entering cover.
            Alert = Math.Max(0f, Alert - seconds * (hidden ? 25f : 14f) / searchPatience);
            if (Alert > 0f || unseenTime < (hidden ? 5f : 9f) * searchPatience) return;
        }
        else Alert = Math.Max(0f, Alert - seconds * 32f / searchPatience);
        Phase = Alert > 0f ? UXAlertPhase.Suspicious : UXAlertPhase.Clear;
    }

    public bool TryDamage(int amount)
    {
        return ApplyDamage(amount, Responding);
    }

    public bool TryHazardDamage(int amount) => ApplyDamage(amount, true);

    // An energized laser is lethal even during a recent shot/mine's brief
    // protection. This exception never changes ordinary guard or mine stacking.
    public bool TryLethalHazard()
    {
        if (!Alive) return false;
        Health = 0;
        return true;
    }

    public void InvestigateNoise()
    {
        if (!Alive || Phase == UXAlertPhase.Engaged) return;
        unseenTime = 0f; Alert = Math.Max(60f, Alert); Phase = UXAlertPhase.Searching;
    }

    private bool ApplyDamage(int amount, bool allowed)
    {
        if (!Alive || !allowed || amount <= 0 || hitProtection > 0f) return false;
        Health = Math.Max(0, Health - amount);
        hitProtection = 0.85f; // Shared across guards: three shots cannot erase all health in one frame.
        return true;
    }
}
