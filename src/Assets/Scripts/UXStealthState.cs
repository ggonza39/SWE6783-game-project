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

    public void Tick(float seconds, bool guardSeesPlayer, bool cameraSeesPlayer, bool hidden)
    {
        if (seconds <= 0f || !Alive) return;
        hitProtection = Math.Max(0f, hitProtection - seconds);
        bool seen = !hidden && (guardSeesPlayer || cameraSeesPlayer);
        if (seen)
        {
            unseenTime = 0f;
            Alert = Math.Min(100f, Alert + seconds * (cameraSeesPlayer ? 125f : 85f));
            Phase = Alert >= 100f || Responding ? UXAlertPhase.Engaged : UXAlertPhase.Suspicious;
            return;
        }

        unseenTime += seconds;
        if (Phase == UXAlertPhase.Engaged && unseenTime < 1.6f) return;
        if (Responding)
        {
            Phase = UXAlertPhase.Searching;
            // Time is measured since line of sight broke, not since entering cover.
            Alert = Math.Max(0f, Alert - seconds * (hidden ? 25f : 14f));
            if (Alert > 0f || unseenTime < (hidden ? 5f : 9f)) return;
        }
        else Alert = Math.Max(0f, Alert - seconds * 32f);
        Phase = Alert > 0f ? UXAlertPhase.Suspicious : UXAlertPhase.Clear;
    }

    public bool TryDamage(int amount)
    {
        if (!Alive || !Responding || amount <= 0 || hitProtection > 0f) return false;
        Health = Math.Max(0, Health - amount);
        hitProtection = 0.85f; // Shared across guards: three shots cannot erase all health in one frame.
        return true;
    }
}
