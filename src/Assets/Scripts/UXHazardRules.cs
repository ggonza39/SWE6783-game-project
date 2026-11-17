using System;

// Deterministic clocks: gameplay time only, no random damage or real-time timers.
public sealed class UXMineState
{
    public readonly float Fuse;
    public bool Armed { get; private set; }
    public bool Spent { get; private set; }
    public bool Neutralized { get; private set; }
    public float Elapsed { get; private set; }
    public float Fraction => Math.Min(1f, Elapsed / Fuse);
    public float Remaining => Math.Max(0f, Fuse - Elapsed);

    // Hard gains a small reaction margin; its blast still costs much more health.
    public UXMineState(bool hard) { Fuse = 1.10f; }

    // Returns true on the single detonation frame. Arming grants the full fuse.
    public bool Tick(float seconds, bool crossedTrigger)
    {
        if (Spent || Neutralized || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
        if (!Armed) { if (crossedTrigger) Armed = true; return false; }
        Elapsed += seconds;
        if (Elapsed < Fuse) return false;
        Armed = false; Spent = true; return true;
    }

    public void Neutralize() { Armed = false; Neutralized = true; }
}

public enum UXLaserPhase { Safe, Warning, Active }

public sealed class UXLaserState
{
    public readonly float SafeSeconds, WarningSeconds, ActiveSeconds;
    public float Clock { get; private set; }
    public bool Neutralized { get; private set; }
    public UXLaserPhase Phase => Neutralized || Clock < SafeSeconds ? UXLaserPhase.Safe :
        Clock < SafeSeconds + WarningSeconds ? UXLaserPhase.Warning : UXLaserPhase.Active;

    public UXLaserState(bool hard, float offset)
    {
        SafeSeconds = hard ? 1.65f : 3.20f;
        WarningSeconds = hard ? .45f : .95f;
        ActiveSeconds = hard ? 2.10f : 1.20f;
        Clock = Math.Max(0f, Math.Min(offset, SafeSeconds * .7f));
    }

    public void Tick(float seconds)
    {
        if (Neutralized || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        Clock = (Clock + seconds) % (SafeSeconds + WarningSeconds + ActiveSeconds);
    }
    public void Neutralize() { Neutralized = true; }

    // Swept collision prevents sprinting across an active strip between frames.
    public static bool Crosses(float ax, float ay, float bx, float by, float x, float y, float w, float h)
    {
        float lo = 0f, hi = 1f;
        return Slab(ax, bx - ax, x - w * .5f, x + w * .5f, ref lo, ref hi) &&
            Slab(ay, by - ay, y - h * .5f, y + h * .5f, ref lo, ref hi);
    }
    private static bool Slab(float at, float step, float min, float max, ref float lo, ref float hi)
    {
        if (Math.Abs(step) < .00001f) return at >= min && at <= max;
        float a = (min - at) / step, b = (max - at) / step;
        lo = Math.Max(lo, Math.Min(a, b)); hi = Math.Min(hi, Math.Max(a, b)); return lo <= hi;
    }
}
